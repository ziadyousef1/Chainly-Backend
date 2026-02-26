using Chainly.Application.DTOs;
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

    Task<IActionResult> DeleteReportAsync(int companyId, int reportId);
}