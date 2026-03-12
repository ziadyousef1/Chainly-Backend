using Chainly.Application.Configurations;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Firebase.Database;
using Firebase.Database.Query;
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
        public int GoodCount { get; set; }
        public int DefectCount { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public IDisposable? FirebaseSubscription { get; set; }
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
            .AsObservable<DetectionEvent>()
            .Subscribe(async detection =>
            {
                try
                {
                    if (detection?.Object == null)
                        return;

                    var detectionData = detection.Object;
                    var isDefect = detectionData.Defect;
                    var timestamp = detectionData.Timestamp;

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
                            reportToUpdate.DefectiveProducts = monitor.DefectCount;
                            reportToUpdate.GoodProducts = monitor.GoodCount;
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
                catch
                {
                }
            });

        monitor.FirebaseSubscription = subscription;
        _activeMonitors.TryAdd(reportId, monitor);
    }

    public async Task StopMonitoringReportAsync(int reportId)
    {
        if (_activeMonitors.TryRemove(reportId, out var monitor))
        {
            monitor.FirebaseSubscription?.Dispose();
        }
        
        await Task.CompletedTask;
    }

    public async Task<IActionResult> GetLiveProductCountAsync(int companyId, int reportId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var report = await context.Reports
            .Include(r => r.ProductionLine)
            .FirstOrDefaultAsync(r => r.Id == reportId && r.ProductionLine.CompanyId == companyId);

        if (report == null)
            return new NotFoundObjectResult(new { message = "Report not found." });

        if (_activeMonitors.TryGetValue(reportId, out var monitor))
        {
            return new OkObjectResult(new
            {
                ReportId = reportId,
                ProductionLineId = report.ProductionLineId,
                ProductionLineName = report.ProductionLine.LineName,
                GoodCount = monitor.GoodCount,
                DefectCount = monitor.DefectCount,
                TotalCount = monitor.GoodCount + monitor.DefectCount,
                IsActive = true,
                LastUpdated = monitor.LastUpdated
            });
        }

        return new OkObjectResult(new
        {
            ReportId = reportId,
            ProductionLineId = report.ProductionLineId,
            ProductionLineName = report.ProductionLine.LineName,
            GoodCount = report.GoodProducts,
            DefectCount = report.DefectiveProducts,
            TotalCount = report.GoodProducts + report.DefectiveProducts,
            IsActive = false,
            LastUpdated = report.EndedAt ?? report.StartedAt
        });
    }
}
