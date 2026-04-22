using Chainly.Application.Configurations;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Microsoft.AspNetCore.Mvc;
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
        public object SyncRoot { get; } = new();
        public int GoodCount { get; set; }
        public int DefectCount { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public IDisposable? DefectSubscription { get; set; }
        public IDisposable? NonDefectSubscription { get; set; }
    }

    private class DetectionEvent
    {
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
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var report = await context.Reports
                .Include(r => r.ProductionLine)
                .FirstOrDefaultAsync(r => r.Id == reportId && r.ProductionLine.CompanyId == companyId);

            if (report == null)
                return;

            productionLineId = report.ProductionLineId;
        }

        var monitor = new ReportMonitor();
        
        var defectSubscription = _firebaseClient
            .Child(reportId.ToString())
            .Child("defect")
            .AsObservable<DetectionEvent>()
            .Subscribe(async detection =>
            {
                await ProcessDetectionAsync(reportId, productionLineId, monitor, detection, true);
            });

        var nonDefectSubscription = _firebaseClient
            .Child(reportId.ToString())
            .Child("non_defect")
            .AsObservable<DetectionEvent>()
            .Subscribe(async detection =>
            {
                await ProcessDetectionAsync(reportId, productionLineId, monitor, detection, false);
            });

        monitor.DefectSubscription = defectSubscription;
        monitor.NonDefectSubscription = nonDefectSubscription;
        _activeMonitors.TryAdd(reportId, monitor);
    }

    private async Task ProcessDetectionAsync(int reportId, int productionLineId, ReportMonitor monitor, FirebaseEvent<DetectionEvent> detection, bool isDefect)
    {
        try
        {
            if (detection?.Object == null || string.IsNullOrWhiteSpace(detection.Key))
                return;

            var timestamp = detection.Object.Timestamp;
            int goodCount;
            int defectCount;
            DateTime lastUpdated;

            lock (monitor.SyncRoot)
            {
                if (isDefect)
                    monitor.DefectCount++;
                else
                    monitor.GoodCount++;

                monitor.LastUpdated = DateTime.UtcNow;
                goodCount = monitor.GoodCount;
                defectCount = monitor.DefectCount;
                lastUpdated = monitor.LastUpdated;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var reportToUpdate = await context.Reports.FindAsync(reportId);
                if (reportToUpdate != null)
                {
                    reportToUpdate.DefectiveProducts = defectCount;
                    reportToUpdate.GoodProducts = goodCount;
                    await context.SaveChangesAsync();
                }
            }

            await _hubContext.Clients.Group($"ProductionLine_{productionLineId}")
                .SendAsync("ProductCountUpdated", new
                {
                    ReportId = reportId,
                    ProductionLineId = productionLineId,
                    GoodCount = goodCount,
                    DefectCount = defectCount,
                    TotalCount = goodCount + defectCount,
                    IsDefect = isDefect,
                    Timestamp = timestamp,
                    LastUpdated = lastUpdated
                });
        }
        catch
        {
        }
    }

    public async Task StopMonitoringReportAsync(int reportId)
    {
        if (_activeMonitors.TryRemove(reportId, out var monitor))
        {
            monitor.DefectSubscription?.Dispose();
            monitor.NonDefectSubscription?.Dispose();
        }
        
        await Task.CompletedTask;
    }

    
}
