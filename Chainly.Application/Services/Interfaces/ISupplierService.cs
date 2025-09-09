using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Chainly.Application.DTOs.Suppliers;

namespace Chainly.Application.Services.Interfaces;

public interface ISupplierService
{
    Task<IActionResult> GetSuppliersAsync(int companyId, int? materialId, int? Quantity = 1, int? pageNumber = null, int? pageSize = null , string? sortType = null);
    Task<IActionResult> GetSupplierAsync(int id, int companyId, int? materialId, int? quantity = 1);
    Task<IActionResult> CreateSupplierAsync(SupplierDto dto, int companyId);
    Task<IActionResult> UpdateSupplierAsync(int id, SupplierDto dto, int companyId);
    Task<IActionResult> DeleteSupplierAsync(int id, int companyId);
    Task<IActionResult> ImportSuppliersAsync(IFormFile file, int? companyId = null);
}