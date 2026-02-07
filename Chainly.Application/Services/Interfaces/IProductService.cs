using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IProductService
{
    Task<IActionResult> GetAllProductsAsync(int companyId, int pageNumber = 1, int pageSize = 10, string? search = null, string? sortByStock = null);
    Task<IActionResult> GetTopProductsAsync(int companyId, int topCount = 5);
    Task<IActionResult> ImportProductsFromExcelAsync(IFormFile file, int companyId);
    Task<IActionResult> DeleteProductAsync(int productId, int companyId);
}