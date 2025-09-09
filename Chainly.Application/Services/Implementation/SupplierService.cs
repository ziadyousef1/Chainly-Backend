using Chainly.Application.DTOs;
using Chainly.Application.DTOs.Suppliers;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Chainly.Domain.Constants.Permissions;

namespace Chainly.Application.Services.Implementation;

public class SupplierService(AppDbContext context, ISupplierImportService supplierImportService)
    : ISupplierService
{
    public async Task<IActionResult> GetSuppliersAsync(int companyId, int? materialId, int? pageNumber, int? pageSize, int? Quantity = 1 , string? sortType = null)
    {
        var company = await context.Companies.FindAsync(companyId);
        if (company == null)
            return new NotFoundObjectResult($"Company {companyId} not found");

        IQueryable<Supplier> query = context.Suppliers
        .Where(s => s.CompanyId == null || s.CompanyId == company.Id)
        .Include(s => s.SupplierMaterials)
        .ThenInclude(sm => sm.Material);

        
        if (!string.IsNullOrEmpty(sortType) && sortType.ToLower() == "carbon")
        {
            if (!materialId.HasValue)
                return new BadRequestObjectResult("materialId is required for carbon sorting.");

            query = query.Where(s => s.SupplierMaterials.Any(sm => sm.MaterialId == materialId.Value));
        }

        var supplierEntities = await query.ToListAsync();

        var suppliers = supplierEntities.Select(s =>
        {
            var distance = CalculateDistance(
                company.LocationLatitude,
                company.LocationLongitude,
                s.LocationLatitude,
                s.LocationLongitude);

            if (string.IsNullOrEmpty(sortType)) 
            {
                return new SupplierDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Latitude = s.LocationLatitude,
                    Longitude = s.LocationLongitude,
                    CompanyId = s.CompanyId,
                    FacilityType = s.FacilityType,
                    Sector = s.Sector,
                    Distance = distance
                };
            }

            
            var supplierMaterial = materialId.HasValue
                ? s.SupplierMaterials.FirstOrDefault(sm => sm.MaterialId == materialId.Value)
                : null;

            decimal? totalCarbon = null;
            string carbonUnit = null;

            if (supplierMaterial != null)
            {
                totalCarbon = distance * supplierMaterial.Material.EmissionValue * (Quantity ?? 1);
                carbonUnit = $"Kg CO2e/{supplierMaterial.Material.Unit}";
            }

            return new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                Latitude = s.LocationLatitude,
                Longitude = s.LocationLongitude,
                CompanyId = s.CompanyId,
                FacilityType = s.FacilityType,
                Sector = s.Sector,
                Distance = distance,
                TotalCarbon = totalCarbon,
                CarbonUnit = carbonUnit
            };
        });

        
        if (!string.IsNullOrEmpty(sortType))
        {
            suppliers = sortType.ToLower() switch
            {
                "carbon" => suppliers.OrderBy(s => s.TotalCarbon).ThenBy(s => s.Distance),
                "distance" => suppliers.OrderBy(s => s.Distance).ThenBy(s => s.TotalCarbon ?? decimal.MaxValue),
                _ => suppliers
            };
        }

        var totalCount = suppliers.Count();

        if (pageNumber.HasValue && pageSize.HasValue && pageNumber > 0 && pageSize > 0)
        {
            suppliers = suppliers
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();

            return new OkObjectResult(new PagedResult<SupplierDto>
            {
                Items = suppliers,
                TotalCount = totalCount,
                PageNumber = pageNumber.Value,
                PageSize = pageSize.Value
            });
        }

        return new OkObjectResult(new PagedResult<SupplierDto>
        {
            Items = suppliers,
            TotalCount = totalCount
        });
    }

    public async Task<IActionResult> GetSupplierAsync(int id, int companyId , int? materialId , int? quantity = 1)
    {
        var supplier = await context.Suppliers
        .Include(s => s.SupplierMaterials)
        .ThenInclude(sm => sm.Material)
        .FirstOrDefaultAsync(s => s.Id == id && (s.CompanyId == companyId || s.CompanyId == null));

        if (supplier == null)
            return new NotFoundObjectResult($"Supplier {id} not found");

        var company = await context.Companies.FindAsync(companyId);
        if (company == null)
            return new NotFoundObjectResult($"Company {companyId} not found");

        var distance = CalculateDistance(
            company.LocationLatitude,
            company.LocationLongitude,
            supplier.LocationLatitude,
            supplier.LocationLongitude);


        decimal? totalCarbon = null;
        string carbonUnit = null;

        
        if (materialId.HasValue)
        {
            var supplierMaterial = supplier.SupplierMaterials
                .FirstOrDefault(sm => sm.MaterialId == materialId.Value);

            if (supplierMaterial != null)
            {
                totalCarbon = distance * supplierMaterial.Material.EmissionValue * (quantity ?? 1);
                carbonUnit = $"Kg CO2e/{supplierMaterial.Material.Unit}";
            }
        }

        var dto = new SupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Latitude = supplier.LocationLatitude,
            Longitude = supplier.LocationLongitude,
            CompanyId = supplier.CompanyId,
            FacilityType = supplier.FacilityType,
            Sector = supplier.Sector,
            Distance = distance,
            TotalCarbon = totalCarbon,
            CarbonUnit = carbonUnit
        };


        return new OkObjectResult(dto);
     
    }

    public async Task<IActionResult> CreateSupplierAsync(SupplierDto dto, int companyId)
    {
        var company = await context.Companies.FindAsync(companyId);
        if (company == null)
            return new NotFoundObjectResult($"Company {companyId} not found");

        var supplier = new Supplier
        {
            Name = dto.Name,
            LocationLatitude = dto.Latitude,
            LocationLongitude = dto.Longitude,
            CompanyId = companyId,
            FacilityType = string.IsNullOrWhiteSpace(dto.FacilityType) ? "Unknown" : dto.FacilityType,
            Sector = string.IsNullOrWhiteSpace(dto.Sector) ? "Other" : dto.Sector
        };

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        return new OkObjectResult(new { Message = "Supplier created successfully", SupplierId = supplier.Id });
    }

    public async Task<IActionResult> UpdateSupplierAsync(int id, SupplierDto dto, int companyId)
    {
        var supplier = await context.Suppliers.FindAsync(id);
        if (supplier == null || supplier.CompanyId != companyId)
            return new NotFoundObjectResult($"Supplier {id} not found");

        supplier.Name = dto.Name;
        supplier.LocationLatitude = dto.Latitude;
        supplier.LocationLongitude = dto.Longitude;
        supplier.FacilityType = string.IsNullOrWhiteSpace(dto.FacilityType) ? supplier.FacilityType : dto.FacilityType;
        supplier.Sector = string.IsNullOrWhiteSpace(dto.Sector) ? supplier.Sector : dto.Sector;

        context.Suppliers.Update(supplier);
        await context.SaveChangesAsync();

        return new OkObjectResult("Supplier updated successfully");
    }

    public async Task<IActionResult> DeleteSupplierAsync(int id, int companyId)
    {
        var supplier = await context.Suppliers.FindAsync(id);
        if (supplier == null || supplier.CompanyId != companyId)
            return new NotFoundObjectResult($"Supplier {id} not found");

        context.Suppliers.Remove(supplier);
        await context.SaveChangesAsync();

        return new OkObjectResult("Supplier deleted successfully");
    }

    private static decimal CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double R = 6371; // Radius of the Earth in km
        var dLat = (double)(lat2 - lat1) * Math.PI / 180.0;
        var dLon = (double)(lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos((double)lat1 * Math.PI / 180.0) *
                Math.Cos((double)lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (decimal)(R * c);
    }


    public async Task<IActionResult> ImportSuppliersAsync(IFormFile file, int? companyId = null)
    {
        if (file == null || file.Length == 0)
            return new BadRequestObjectResult("No file uploaded");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return new BadRequestObjectResult("Only Excel files supported");

        var tempFilePath = Path.GetTempFileName();
        tempFilePath = Path.ChangeExtension(tempFilePath, ".xlsx");

        using (var stream = new FileStream(tempFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var suppliers = await supplierImportService.ReadSuppliersFromExcelAsync(tempFilePath);

        if (!suppliers.Any())
            return new BadRequestObjectResult("No valid data found");

        var isValid = await supplierImportService.ValidateSupplierDataAsync(suppliers);

        if (!isValid)
            return new BadRequestObjectResult("Validation failed");

        var importedCount = await supplierImportService.ImportSuppliersAsync(suppliers, companyId);

        if (System.IO.File.Exists(tempFilePath))
            System.IO.File.Delete(tempFilePath);

        return new OkObjectResult(new { importedCount, totalRecords = suppliers.Count() });
    }
}