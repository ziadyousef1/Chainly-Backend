using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IFirebaseRealtimeService
{
    Task StartMonitoringReportAsync(int companyId, int reportId);
    Task StopMonitoringReportAsync(int reportId);
    Task<IActionResult> GetLiveProductCountAsync(int companyId, int reportId);
}
