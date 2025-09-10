using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IProductService
{
    Task<IActionResult> GetAllProductsAsync(int companyId, int pageNumber = 1, int pageSize = 10, string? search = null);
    Task<IActionResult> GetTopProductsAsync(int companyId, int topCount = 5);
}