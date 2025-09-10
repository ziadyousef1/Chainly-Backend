namespace Chainly.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController(IProductService productService) : BaseController
{
    [HttpGet]
    [Authorize(Permissions.Products.View)]
    public async Task<IActionResult> GetAllProducts(int pageNumber = 1, int pageSize = 10, string? search = null)
    {
        int companyId = GetCompanyId();
        return await productService.GetAllProductsAsync(companyId, pageNumber, pageSize, search);
    }

    [HttpGet("top/{count}")]
    [Authorize(Permissions.Products.View)]
    public async Task<IActionResult> GetTopProducts(int count)
    {
        int companyId = GetCompanyId();
        return await productService.GetTopProductsAsync(companyId, count);
    }
}