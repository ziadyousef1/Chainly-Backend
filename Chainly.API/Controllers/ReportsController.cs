using Chainly.Application.DTOs.AiInsights;
using Chainly.Application.DTOs.Reports;

namespace Chainly.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportsController(IReportService reportService, IAiInsightsService aiInsightsService, IFirebaseRealtimeService firebaseRealtimeService)
    : BaseController
{
    [HttpGet]
    [Authorize(Permissions.Reports.View)]
    public async Task<IActionResult> GetAllReports(
        int pageNumber = 1,
        int pageSize = 10,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? productionLineId = null,
        string? search = null)
    {
        int companyId = GetCompanyId();
        return await reportService.GetAllReportsAsync(companyId, pageNumber, pageSize, startDate, endDate,
            productionLineId, search);
    }

    [HttpGet("production-line/{productionLineId}")]
    [Authorize(Permissions.Reports.View)]
    public async Task<IActionResult> GetProductionLineReport(
        int productionLineId,
        int pageNumber = 1,
        int pageSize = 10,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        int companyId = GetCompanyId();
        return await reportService.GetProductionLineReport(companyId, productionLineId, pageNumber, pageSize, startDate,
            endDate);
    }

    [HttpGet("production-line/{productionLineId}/reports")]
    [Authorize(Permissions.Reports.View)]
    public async Task<IActionResult> GetProductionLineReports(
        int productionLineId,
        int pageNumber = 1,
        int pageSize = 10,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        int companyId = GetCompanyId();
        return await reportService.GetProductionLineReportsAsync(companyId, productionLineId, pageNumber, pageSize, startDate,
            endDate);
    }


    [HttpGet("{reportId}")]
    [Authorize(Permissions.Reports.View)]
    public async Task<IActionResult> GetReportById(int reportId)
    {
        int companyId = GetCompanyId();
        var result = await reportService.GetReportSummaryByIdAsync(companyId, reportId);

        if (result is OkObjectResult)
        {
            await firebaseRealtimeService.StartMonitoringReportAsync(companyId, reportId);
        }

        return result;
    }

    [HttpGet("monthly-summary")]
    [Authorize]
    public async Task<IActionResult> GetMonthlyProductionLineSummary(int? month = null, int? year = null,
        int pageNumber = 1, int pageSize = 10)
    {
        var companyId = GetCompanyId();
        return await reportService.GetMonthlyReportSummaryAsync(companyId, month, year, pageNumber, pageSize);
    }

    [HttpPost("{reportId}/add-products")]
    [Authorize(Permissions.Reports.Update)]
    public async Task<IActionResult> AddGoodProducts(int reportId, bool defect, int? count = 1)
    {
        return await reportService.AddProductsAsync(reportId, defect, count);
    }
    
    [HttpPost("{reportId}/stop-realtime")]
    [Authorize(Permissions.Reports.Update)]
    public async Task<IActionResult> StopRealtimeMonitoring(int reportId)
    {
        await firebaseRealtimeService.StopMonitoringReportAsync(reportId);
        return Ok(new { message = "Real-time monitoring stopped", report_id = reportId });
    }


    [HttpDelete("{reportId}")]
    [Authorize(Permissions.Reports.Delete)]
    public async Task<IActionResult> DeleteReport(int reportId)
    {
        int companyId = GetCompanyId();
        return await reportService.DeleteReportAsync(companyId, reportId);
    }

    [HttpPost("/video-reports")]
    [Authorize(Permissions.Reports.Create)]
    public async Task<IActionResult> StoreVideoReport([FromBody] CreateVideoReportDto dto)
    {
        int companyId = GetCompanyId();
        return await reportService.StoreVideoReportAsync(companyId, dto);
    }

    [HttpGet("/video-reports")]
    [Authorize(Permissions.Reports.View)]
    public async Task<IActionResult> GetAllVideoReports(string? search, DateTime? startDate,
        DateTime? endDate, int pageNumber = 1, int pageSize = 10)
    {
        int companyId = GetCompanyId();
        return await reportService.GetAllVideoReportsAsync(companyId, search, startDate, endDate, pageNumber, pageSize);
    }

    [HttpGet("/video-reports/{reportId}")]
    [Authorize(Permissions.Reports.View)]
    public async Task<IActionResult> GetVideoReportById(int reportId)
    {
        int companyId = GetCompanyId();
        return await reportService.GetVideoReportByIdAsync(companyId, reportId);
    }

    [HttpGet("/video-reports/insights")]
    [Authorize(Permissions.Reports.View)]
    public async Task<IActionResult> GetVideoReportsInsights(int? productionLineId, DateTime? startDate,
        DateTime? endDate, int pageNumber = 1, int pageSize = 10)
    {
        int companyId = GetCompanyId();

        return await reportService.GetVideoReportsInsights(companyId, productionLineId, startDate, endDate, pageNumber,
            pageSize);
    }

    [HttpPost("start-session")]
    [Authorize(Permissions.Reports.Create)]
    public async Task<IActionResult> StartSession(int productionLineId)
    {
        var dto = new StartSessionRequestDto()
        {
            CompanyId = GetCompanyId(),
            ProductionLineId = productionLineId
        };

        return await aiInsightsService.StartSessionAsync(dto);
    }

    [HttpPost("{reportId}/close-session")]
    [Authorize(Permissions.Reports.Update)]
    public async Task<IActionResult> CloseSession(int reportId)
    {
        var dto = new CloseSessionRequestDto()
        {
            CompanyId = GetCompanyId(),
            ReportId = reportId
        };
        var result = await aiInsightsService.CloseSessionAsync(dto);

        if (result is OkObjectResult)
        {
            await firebaseRealtimeService.StopMonitoringReportAsync(reportId);
        }

        return result;
    }
}