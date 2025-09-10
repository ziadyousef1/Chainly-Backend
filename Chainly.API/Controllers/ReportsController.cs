namespace Chainly.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportsController(IReportService reportService, IAiInsightsService aiInsightsService)
    : BaseController
{
    [HttpPost("store-session/{id}/production-line/{productionLineId}")]
    [Authorize(Permissions.Reports.Create)]
    public async Task<IActionResult> StoreSessionDetails(string id, int productionLineId)
    {
        var sessionDetails = await aiInsightsService.GetSessionDetailsAsync(id);

        if (sessionDetails == null)
            return NotFound(new { message = "Session details not found" });

        return await reportService.StoreSessionDetailsAsync(sessionDetails, productionLineId);
    }

    [HttpGet]
    [Authorize(Permissions.Reports.View)]
    public async Task<IActionResult> GetAllReports(
        int pageNumber = 1,
        int pageSize = 10,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        int companyId = GetCompanyId();
        return await reportService.GetAllReportsAsync(companyId, pageNumber, pageSize, startDate, endDate);
    }

    [HttpGet("{reportId}")]
    [Authorize(Permissions.Reports.View)]
    public async Task<IActionResult> GetReportById(int reportId, int pageNumber = 1, int pageSize = 10)
    {
        int companyId = GetCompanyId();
        return await reportService.GetReportByIdAsync(reportId, companyId, pageNumber, pageSize);
    }

    [HttpGet("summary")]
    [Authorize(Permissions.Reports.View)]
    public async Task<IActionResult> GetReportSummary(
        int? productionLineId,
        DateTime? startDate,
        DateTime? endDate)
    {
        int companyId = GetCompanyId();
        return await reportService.GetReportSummaryAsync(companyId, productionLineId, startDate, endDate);
    }
}