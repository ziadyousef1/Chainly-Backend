using Chainly.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IReportService
{
    Task<IActionResult> StoreSessionDetailsAsync(SessionDetailsResponseDto sessionDto, int productionLineId);
    Task<IActionResult> GetAllReportsAsync(int companyId, int pageNumber = 1, int pageSize = 10,
        DateTime? startDate = null, DateTime? endDate = null);
    Task<IActionResult> GetReportByIdAsync(int reportId, int companyId, int pageNumber = 1, int pageSize = 10);
}