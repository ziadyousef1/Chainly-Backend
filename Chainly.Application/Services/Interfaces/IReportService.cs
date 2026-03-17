using Chainly.Application.DTOs;
using Chainly.Application.DTOs.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IReportService
{
    Task<IActionResult> GetAllReportsAsync(int companyId, int pageNumber = 1, int pageSize = 10,
        DateTime? startDate = null, DateTime? endDate = null, int? productionLineId = null, string? search = null);

    Task<IActionResult> GetMonthlyReportSummaryAsync(int companyId, int? month = null, int? year = null,
        int pageNumber = 1, int pageSize = 10);

    Task<IActionResult> AddProductsAsync(int reportId, bool defect, int? count = 1);

    Task<IActionResult> GetProductionLineReport(int companyId, int productionLineId, int pageNumber = 1,
        int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null);

    Task<IActionResult> GetProductionLineReportsAsync(int companyId, int productionLineId, int pageNumber = 1,
        int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null);


    Task<IActionResult> GetReportSummaryByIdAsync(int companyId, int reportId);

    Task<IActionResult> DeleteReportAsync(int companyId, int reportId);
    Task<IActionResult> StoreVideoReportAsync(int companyId, CreateVideoReportDto dto);

    Task<IActionResult> GetAllVideoReportsAsync(int companyId, string? search = null, DateTime? startDate = null,
        DateTime? endDate = null, int pageNumber = 1, int pageSize = 10);

    Task<IActionResult> GetVideoReportByIdAsync(int companyId, int reportId);

    Task<IActionResult> GetVideoReportsInsights(int companyId, int? productionLineId = null, DateTime? startDate = null,
        DateTime? endDate = null, int pageNumber = 1, int pageSize = 10);
    
    Task<IActionResult> GetProductionLines(int companyId, int pageNumber = 1, int pageSize = 10, string? search = null);
}