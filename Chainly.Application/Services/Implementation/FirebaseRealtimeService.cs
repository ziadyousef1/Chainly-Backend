using Chainly.Application.Configurations;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reactive.Linq;

namespace Chainly.Application.Services.Implementation;

public class FirebaseRealtimeService : IFirebaseRealtimeService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<Chainly.Infrastructure.Hubs.ReportHub> _hubContext;
    private readonly FirebaseSettings _firebaseSettings;
    private readonly FirebaseClient _firebaseClient;
    private static readonly ConcurrentDictionary<int, ReportMonitor> _activeMonitors = new();

    private class ReportMonitor
    {
        public int GoodCount { get; set; }
        public int DefectCount { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public IDisposable? FirebaseSubscription { get; set; }

        public readonly HashSet<string> PreloadedKeys = new(StringComparer.Ordinal);
        private readonly HashSet<string> _processedKeys = new(StringComparer.Ordinal);
        private readonly object _lock = new();

        public bool TryProcess(string key)
        {
            lock (_lock)
            {
                if (PreloadedKeys.Contains(key)) return false;
                return _processedKeys.Add(key);
            }
        }
    }

    private class DetectionEvent
    {
        public bool Defect { get; set; }
        public string Timestamp { get; set; } = string.Empty;
    }

    public FirebaseRealtimeService(
        IServiceScopeFactory scopeFactory,
        IHubContext<Chainly.Infrastructure.Hubs.ReportHub> hubContext,
        IOptions<FirebaseSettings> firebaseSettings)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _firebaseSettings = firebaseSettings.Value;
        _firebaseClient = new FirebaseClient(_firebaseSettings.DatabaseUrl);
    }

    public async Task StartMonitoringReportAsync(int companyId, int reportId)
    {
        if (_activeMonitors.ContainsKey(reportId))
            return;

        int productionLineId;
        var monitor = new ReportMonitor();

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var report = await context.Reports
                .Include(r => r.ProductionLine)
                .FirstOrDefaultAsync(r => r.Id == reportId && r.ProductionLine.CompanyId == companyId);

            if (report == null)
                return;

            productionLineId = report.ProductionLineId;
            monitor.GoodCount = report.GoodProducts;
            monitor.DefectCount = report.DefectiveProducts;
        }

        try
        {
            using var snapshotClient = new FirebaseClient(_firebaseSettings.DatabaseUrl);
            var snapshotTask = snapshotClient
                .Child(reportId.ToString())
                .Child("detections")
                .OnceAsync<DetectionEvent>();

            if (await Task.WhenAny(snapshotTask, Task.Delay(TimeSpan.FromSeconds(3))) == snapshotTask)
                foreach (var item in await snapshotTask)
                    monitor.PreloadedKeys.Add(item.Key);
        }
        catch { }

        var subscription = _firebaseClient
            .Child(reportId.ToString())
            .Child("detections")
            .AsObservable<DetectionEvent>()
            .Subscribe(async detection =>
            {
                try
                {
                    if (detection?.Object == null)
                        return;

                    if (!string.IsNullOrEmpty(detection.Key) && !monitor.TryProcess(detection.Key))
                        return;

                    var isDefect = detection.Object.Defect;
                    var timestamp = detection.Object.Timestamp;

                    if (isDefect)
                        monitor.DefectCount++;
                    else
                        monitor.GoodCount++;

                    monitor.LastUpdated = DateTime.UtcNow;

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var reportToUpdate = await context.Reports.FindAsync(reportId);
                        if (reportToUpdate != null)
                        {
                            reportToUpdate.GoodProducts = monitor.GoodCount;
                            reportToUpdate.DefectiveProducts = monitor.DefectCount;
                            await context.SaveChangesAsync();
                        }
                    }

                    await _hubContext.Clients.Group($"ProductionLine_{productionLineId}")
                        .SendAsync("ProductCountUpdated", new
                        {
                            ReportId = reportId,
                            ProductionLineId = productionLineId,
                            GoodCount = monitor.GoodCount,
                            DefectCount = monitor.DefectCount,
                            TotalCount = monitor.GoodCount + monitor.DefectCount,
                            IsDefect = isDefect,
                            Timestamp = timestamp,
                            LastUpdated = monitor.LastUpdated
                        });
                }
                catch { }
            });

        monitor.FirebaseSubscription = subscription;

        if (!_activeMonitors.TryAdd(reportId, monitor))
            subscription.Dispose();
    }

    public async Task StopMonitoringReportAsync(int reportId)
    {
        if (_activeMonitors.TryRemove(reportId, out var monitor))
            monitor.FirebaseSubscription?.Dispose();

        await Task.CompletedTask;
    }
}
