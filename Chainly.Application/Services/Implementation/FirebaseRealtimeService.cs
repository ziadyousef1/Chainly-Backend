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
        private int _goodCount;
        private int _defectCount;
        private long _lastUpdated = DateTime.UtcNow.Ticks;
        
        public int GoodCount
        {
            get => Volatile.Read(ref _goodCount);
            set => Volatile.Write(ref _goodCount, value);
        }
        
        public int DefectCount
        {
            get => Volatile.Read(ref _defectCount);
            set => Volatile.Write(ref _defectCount, value);
        }
        
        public DateTime LastUpdated
        {
            get => new DateTime(Volatile.Read(ref _lastUpdated));
            set => Volatile.Write(ref _lastUpdated, value.Ticks);
        }
        
        public IDisposable? Subscription { get; set; }
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
        
        var subscription = _firebaseClient
            .Child(reportId.ToString())
            .Child("detections")
            .AsObservable<Dictionary<string, dynamic>>()
            .Subscribe(async detection =>
            {
                await ProcessDetectionsAsync(reportId, productionLineId, monitor, detection);
            });

        monitor.Subscription = subscription;
        _activeMonitors.TryAdd(reportId, monitor);
    }

    private async Task ProcessDetectionsAsync(int reportId, int productionLineId, ReportMonitor monitor, FirebaseEvent<Dictionary<string, dynamic>> detection)
    {
        try
        {
            if (detection?.Object == null)
                return;

            int goodCount = 0;
            int defectCount = 0;

            foreach (var kvp in detection.Object)
            {
                if (kvp.Value is Dictionary<string, dynamic> itemDict && 
                    itemDict.TryGetValue("defect", out var defectObj))
                {
                    if (Convert.ToBoolean(defectObj))
                        defectCount++;
                    else
                        goodCount++;
                }
            }

            monitor.DefectCount = defectCount;
            monitor.GoodCount = goodCount;
            monitor.LastUpdated = DateTime.UtcNow;

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
                    LastUpdated = monitor.LastUpdated
                });
        }
        catch
        {
        }
    }

    public async Task UpdateDetectionDefectStatusAsync(int reportId, string detectionId, bool isDefect)
    {
        try
        {
            await _firebaseClient
                .Child(reportId.ToString())
                .Child("detections")
                .Child(detectionId)
                .Child("defect")
                .PutAsync(isDefect);
        }
        catch
        {
        }
    }

    public async Task StopMonitoringReportAsync(int reportId)
    {
        if (_activeMonitors.TryRemove(reportId, out var monitor))
        {
            monitor.Subscription?.Dispose();
        }
        
        await Task.CompletedTask;
    }

    
}
