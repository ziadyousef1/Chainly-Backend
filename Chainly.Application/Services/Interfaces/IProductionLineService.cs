using Chainly.Application.DTOs.ProductionLines;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IProductionLineService
{
    Task<IActionResult> GetAllProductionLinesAsync(int companyId, int pageNumber = 1, int pageSize = 10);
    Task<IActionResult> GetProductionLineByIdAsync(int id, int companyId);
    Task<IActionResult> CreateProductionLineAsync(ProductionLineCreateDto dto, int companyId);
    Task<IActionResult> UpdateProductionLineAsync(int id, int companyId, ProductionLineUpdateDto dto);
    Task<IActionResult> DeleteProductionLineAsync(int id, int companyId);
}