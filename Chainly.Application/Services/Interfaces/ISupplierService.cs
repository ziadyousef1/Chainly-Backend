using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Chainly.Application.DTOs.Suppliers;

namespace Chainly.Application.Services.Interfaces;

public interface ISupplierService
{
    Task<IActionResult> GetSuppliersAsync(int companyId, int pageNumber = 1, int pageSize = 10,
        string? sortType = null, string? search = null);

    Task<IActionResult> GetSupplierAsync(int id, int companyId, int pageNumber = 1, int pageSize = 10,
        string? search = null);

    Task<IActionResult> CreateSupplierAsync(SupplierCreateDto dto, int companyId);
    Task<IActionResult> UpdateSupplierAsync(int id, SupplierUpdateDto dto, int companyId);
    Task<IActionResult> DeleteSupplierAsync(int id, int companyId);
    Task<IActionResult> ImportSuppliersAsync(IFormFile file, int? companyId = null);
}