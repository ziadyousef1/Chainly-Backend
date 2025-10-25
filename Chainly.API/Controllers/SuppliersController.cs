using Chainly.Application.DTOs.Suppliers;
using Chainly.Application.Services.Interfaces;
using Chainly.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController(ISupplierService supplierService, ISupplierImportService supplierImportService)
    : BaseController
{
    [HttpGet]
    [Authorize(Permissions.Suppliers.View)]
    public async Task<IActionResult> GetAllSuppliers([FromQuery] int pageNumber, [FromQuery] int pageSize,
        [FromQuery] string sortType = null, [FromQuery] string? search = null)
    {
        int companyId = GetCompanyId();
        return await supplierService.GetSuppliersAsync(companyId, pageNumber, pageSize, sortType, search);
    }

    [HttpGet("{id}")]
    [Authorize(Permissions.Suppliers.View)]
    public async Task<IActionResult> GetSupplier(int id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        int companyId = GetCompanyId();
        return await supplierService.GetSupplierAsync(id, companyId, pageNumber, pageSize, search);
    }


    [HttpPost]
    [Authorize(Permissions.Suppliers.Create)]
    public async Task<IActionResult> CreateSupplier([FromBody] SupplierCreateDto dto)
    {
        int companyId = GetCompanyId();
        return await supplierService.CreateSupplierAsync(dto, companyId);
    }

    [HttpPut("{id}")]
    [Authorize(Permissions.Suppliers.Update)]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SupplierUpdateDto dto)
    {
        int companyId = GetCompanyId();
        return await supplierService.UpdateSupplierAsync(id, dto, companyId);
    }

    [HttpDelete("{id}")]
    [Authorize(Permissions.Suppliers.Delete)]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        int companyId = GetCompanyId();
        return await supplierService.DeleteSupplierAsync(id, companyId);
    }

    [HttpPost("import-suppliers")]
    [Authorize(Permissions.Suppliers.Import)]
    public async Task<IActionResult> ImportSuppliers(IFormFile file, [FromQuery] int? companyId = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        var filePath = Path.GetTempFileName();
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var suppliers = await supplierImportService.ReadSuppliersFromExcelAsync(filePath);

        var isValid = await supplierImportService.ValidateSupplierDataAsync(suppliers);
        if (!isValid) return BadRequest("Invalid supplier data.");

        var count = await supplierImportService.ImportSuppliersAsync(suppliers, companyId);
        return Ok(new { Message = $"Imported {count} suppliers successfully." });
    }

    [HttpPost("import-materials")]
    [Authorize(Permissions.Suppliers.Import)]
    public async Task<IActionResult> ImportMaterials(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        var filePath = Path.GetTempFileName();
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var materials = await supplierImportService.ReadMaterialsFromExcelAsync(filePath);

        foreach (var m in materials)
        {
            Console.WriteLine($"Name: '{m.Name}', Unit: '{m.Unit}', EmissionValue: {m.EmissionValue}");
        }

        var isValid = await supplierImportService.ValidateMaterialDataAsync(materials);
        if (!isValid)
            return BadRequest("Invalid material data. Check console logs for details.");

        var count = await supplierImportService.ImportMaterialsAsync(materials);
        return Ok(new { Message = $"Imported {count} materials successfully." });
    }

    [HttpPost("import-supplier-materials")]
    [Authorize(Permissions.Suppliers.Import)]
    public async Task<IActionResult> ImportSupplierMaterials(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        var filePath = Path.GetTempFileName();
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var supplierMaterials = await supplierImportService.ReadSupplierMaterialsFromExcelAsync(filePath);

        var isValid = await supplierImportService.ValidateSupplierMaterialDataAsync(supplierMaterials);
        if (!isValid) return BadRequest("Invalid supplier-material relation data.");

        var count = await supplierImportService.ImportSupplierMaterialsAsync(supplierMaterials);
        return Ok(new { Message = $"Linked {count} supplier-material relations successfully." });
    }

    [HttpPost("import-complete-excel")]
    [Authorize(Permissions.Suppliers.Import)]
    public async Task<IActionResult> ImportCompleteExcelFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Excel file is required.");

        if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            return BadRequest("Only Excel files (.xlsx, .xls) are supported.");

        try
        {
            int companyId = GetCompanyId();
            var filePath = Path.GetTempFileName();
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var result = await supplierImportService.ImportAllDataFromExcelAsync(filePath, companyId);
            int suppliersImported = result.SuppliersImported;
            int materialsImported = result.MaterialsImported;
            int relationshipsImported = result.RelationshipsImported;

            System.IO.File.Delete(filePath);

            return Ok(new
            {
                Message = "Complete Excel import successful!",
                Data = new
                {
                    SuppliersImported = suppliersImported,
                    MaterialsImported = materialsImported,
                    RelationshipsImported = relationshipsImported,
                    TotalRecords = suppliersImported + materialsImported + relationshipsImported,
                    CompanyId = companyId
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Import failed", Error = ex.Message });
        }
    }
}