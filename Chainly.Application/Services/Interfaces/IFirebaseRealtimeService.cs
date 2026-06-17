using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IFirebaseRealtimeService
{
    Task StartMonitoringReportAsync(int companyId, int reportId);
    Task StopMonitoringReportAsync(int reportId);
    Task UpdateDetectionDefectStatusAsync(int reportId, string detectionId, bool isDefect);
}
