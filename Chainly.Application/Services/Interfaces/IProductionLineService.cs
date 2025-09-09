using Chainly.Application.DTOs.ProductionLines;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IProductionLineService
{
    Task<IActionResult> GetAllAsync(int companyId, int pageNumber = 1, int pageSize = 10);
    Task<IActionResult> GetAsync(int id, int companyId);
    Task<IActionResult> CreateAsync(ProductionLineCreateDto dto, int companyId);
    Task<IActionResult> UpdateAsync(int id, int companyId, ProductionLineUpdateDto dto);
    Task<IActionResult> DeleteAsync(int id, int companyId);
}