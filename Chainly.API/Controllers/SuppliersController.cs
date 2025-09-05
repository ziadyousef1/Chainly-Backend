using Chainly.Application.DTOs;
using Chainly.Application.Services.Interfaces;
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
    public async Task<IActionResult> GetAllSuppliers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        int companyId = GetCompanyId();
        return await supplierService.GetSuppliersAsync(companyId, pageNumber, pageSize);
    }

    [HttpGet("{id}")]
    [Authorize(Permissions.Suppliers.View)]
    public async Task<IActionResult> GetSupplier(int id)
    {
        int companyId = GetCompanyId();
        return await supplierService.GetSupplierAsync(id, companyId);
    }

    [HttpPost]
    [Authorize(Permissions.Suppliers.Create)]
    public async Task<IActionResult> CreateSupplier([FromBody] SupplierDto dto)
    {
        int companyId = GetCompanyId();
        return await supplierService.CreateSupplierAsync(dto, companyId);
    }

    [HttpPut("{id}")]
    [Authorize(Permissions.Suppliers.Update)]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SupplierDto dto)
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

    [HttpPost("import")]
    [Authorize(Permissions.Suppliers.Import)]
    public async Task<IActionResult> ImportSuppliers(IFormFile file, [FromQuery] int? companyId = null)
    {
        return await supplierService.ImportSuppliersAsync(file, companyId);
    }
}