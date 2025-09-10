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

    [HttpPost("import")]
   [Authorize(Permissions.Products.Import)]
    public async Task<IActionResult> ImportProducts(IFormFile file , int companyId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Please select a valid Excel file.");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only Excel files (.xlsx) are supported.");

        return await productService.ImportProductsFromExcelAsync(file, companyId);
    }
}