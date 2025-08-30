using Chainly.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierImportService _supplierImportService;

        public SuppliersController(ISupplierImportService supplierImportService)
        {
            _supplierImportService = supplierImportService;
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
    }
}
