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

    [HttpGet("monthly-summary")]
    [Authorize]
    public async Task<IActionResult> GetMonthlyProductionLineSummary(int? month = null, int? year = null,
        int pageNumber = 1, int pageSize = 10)
    {
        var companyId = GetCompanyId();
        return await reportService.GetMonthlyReportSummaryAsync(companyId, month, year, pageNumber, pageSize);
    }

    [HttpPost("{reportId}/increment-good-products")]
    public async Task<IActionResult> IncrementGoodProducts(int reportId, [FromQuery] int count = 1)
    {
        var companyId = GetCompanyId();
        return await reportService.IncrementGoodProductsAsync(companyId, reportId, count);
    }

    [HttpPost("{reportId}/add-defective-products")]
    public async Task<IActionResult> AddDefectiveProducts(int reportId,
        [FromBody] DefectiveProductDto[] defectiveProducts)
    {
        var companyId = GetCompanyId();
        return await reportService.AddDefectiveProductsAsync(companyId, reportId, defectiveProducts);
    }

    [HttpPost("create-empty-session")]
    public async Task<IActionResult> CreateEmptySession([FromQuery] string sessionId, [FromQuery] int productionLineId)
    {
        var companyId = GetCompanyId();
        return await reportService.CreateEmptySessionAsync(companyId, sessionId, productionLineId);
    }
}