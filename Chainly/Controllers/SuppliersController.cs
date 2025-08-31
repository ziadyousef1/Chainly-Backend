
namespace Chainly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierImportService _supplierImportService;
        private readonly ISupplierService _service;

        public SuppliersController(ISupplierImportService supplierImportService
            , ISupplierService service)
        {
            _supplierImportService = supplierImportService;
            _service = service;
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportSuppliers(IFormFile file, [FromQuery] int? companyId = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only Excel files supported");

            var tempFilePath = Path.GetTempFileName();
            tempFilePath = Path.ChangeExtension(tempFilePath, ".xlsx");

            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var suppliers = await _supplierImportService.ReadSuppliersFromExcelAsync(tempFilePath);
            
            if (!suppliers.Any())
                return BadRequest("No valid data found");

            var isValid = await _supplierImportService.ValidateSupplierDataAsync(suppliers);
            
            if (!isValid)
                return BadRequest("Validation failed");

            var importedCount = await _supplierImportService.ImportSuppliersAsync(suppliers, companyId);

            if (System.IO.File.Exists(tempFilePath))
                System.IO.File.Delete(tempFilePath);

            return Ok(new { importedCount, totalRecords = suppliers.Count() });
        }
        [HttpGet("{companyId?}")]
        public async Task<IActionResult> GetSuppliers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            var suppliers = await _service.GetSuppliersAsync(userIdClaim, pageNumber, pageSize);
            return Ok(suppliers);
        }

        [HttpPost("{companyId}")]
        public async Task<IActionResult> CreateSupplier(int companyId, [FromBody] SupplierDto dto)
        {
            var created = await _service.CreateSupplierAsync(dto, companyId);
            if (created == null)
                return NotFound($"Company with id {companyId} not found");

            return CreatedAtAction(nameof(GetSuppliers), new { companyId = companyId }, created);
        }

        [HttpPut("{companyId}/{id}")]
        public async Task<IActionResult> UpdateSupplier(int companyId, int id, [FromBody] SupplierDto dto)
        {
            var updated = await _service.UpdateSupplierAsync(id, dto, companyId);
            if (!updated) return NotFound($"Supplier {id} not found or does not belong to Company {companyId}");

            return Ok("Supplier Updated Successfully");
        }
        [HttpDelete("{companyId}/{id}")]
        public async Task<IActionResult> DeleteSupplier(int companyId, int id)
        {
            var deleted = await _service.DeleteSupplierAsync(id, companyId);
            if (!deleted) return NotFound($"Supplier {id} not found or does not belong to Company {companyId}");

            return Ok("Supplier Deleted");
        }


    }
}
