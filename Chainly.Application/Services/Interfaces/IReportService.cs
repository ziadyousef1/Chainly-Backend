using Chainly.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IReportService
{
    Task<IActionResult> StoreSessionDetailsAsync(SessionDetailsResponseDto sessionDto, int productionLineId);
    Task<IActionResult> GetAllReportsAsync(int companyId, int pageNumber = 1, int pageSize = 10,
        DateTime? startDate = null, DateTime? endDate = null);
    Task<IActionResult> GetReportByIdAsync(int reportId, int companyId, int pageNumber = 1, int pageSize = 10);
    Task<IActionResult> GetReportSummaryAsync(int companyId, int? productionLineId = null, DateTime? startDate = null,
        DateTime? endDate = null, int pageNumber = 1, int pageSize = 10);
    Task<IActionResult> GetMonthlyReportSummaryAsync(int companyId, int? month = null, int? year = null,
        int pageNumber = 1, int pageSize = 10);
    Task<IActionResult> IncrementGoodProductsAsync(int companyId, int reportId, int count = 1);
    Task<IActionResult> AddDefectiveProductsAsync(int companyId, int reportId, DefectiveProductDto[] defectiveProducts);
    Task<IActionResult> CreateEmptySessionAsync(int companyId, string sessionId, int productionLineId);
}