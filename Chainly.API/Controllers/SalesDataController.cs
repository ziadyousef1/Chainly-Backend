namespace Chainly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesDataController : BaseController
{
    private readonly ISalesDataService _salesDataService;

    public SalesDataController(ISalesDataService salesDataService)
    {
        _salesDataService = salesDataService;
    }


    [HttpPost("import")]
    public async Task<ActionResult<ServiceResponse>> ImportSalesData(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ServiceResponse("No file uploaded", false));
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                return BadRequest(new ServiceResponse("Only Excel files (.xlsx, .xls) are allowed", false));
            }

            using var stream = file.OpenReadStream();
            
            var salesDataList = await _salesDataService.ReadSalesDataFromExcelAsync(stream);
            
            if (!salesDataList.Any())
            {
                return BadRequest(new ServiceResponse("No valid data found in the file", false));
            }

            var importedCount = await _salesDataService.ImportSalesDataAsync(salesDataList);
            
            return Ok(new ServiceResponse($"Successfully imported {importedCount} sales data records", true));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ServiceResponse($"Error importing data: {ex.Message}", false));
        }
    }


    [HttpGet("product/{productId}/last30days")]
    public async Task<ActionResult<List<SalesDataResponseDto>>> GetLast30DaysForProduct(long productId)
    {
        try
        {
            if (productId <= 0)
            {
                return BadRequest(new ServiceResponse("Invalid product ID provided", false));
            }

            var salesData = await _salesDataService.GetLast30DaysForProductAsync(productId);
            
            if (!salesData.Any())
            {
                return NotFound(new ServiceResponse("No sales data found for this product in the last 30 days", false));
            }

            return Ok(salesData);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ServiceResponse($"Error retrieving sales data: {ex.Message}", false));
        }
    }
}
