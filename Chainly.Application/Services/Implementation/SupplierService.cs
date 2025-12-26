using Chainly.Application.DTOs;
using Chainly.Application.DTOs.Materials;
using Chainly.Application.DTOs.Suppliers;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Chainly.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class SupplierService(
    AppDbContext context,
    ISupplierImportService supplierImportService,
    IExternalApiService externalApiService)
    : ISupplierService
{
    public async Task<IActionResult> GetSuppliersAsync(
        int companyId,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortType = null,
        string? search = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var company = await context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId);

        if (company == null)
            return new NotFoundObjectResult("Company not found.");

        IQueryable<Supplier> query = context.Suppliers
            .AsNoTracking()
            .Where(s => s.CompanyId == null || s.CompanyId == company.Id);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var suppliers = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.LocationLatitude,
                s.LocationLongitude
            })
            .ToListAsync();

        var supplierDtos = new List<object>(suppliers.Count);

        foreach (var supplier in suppliers)
        {
            var distance = LocationHelper.CalculateDistance(
                company.LocationLatitude,
                company.LocationLongitude,
                supplier.LocationLatitude,
                supplier.LocationLongitude);

            supplierDtos.Add(new
            {
                supplier.Id,
                supplier.Name,
                Distance = $"{Math.Round(distance, 2)} Km",
                Location = await GetLocationAsync(
                    supplier.LocationLatitude,
                    supplier.LocationLongitude)
            });
        }

        if (!string.IsNullOrWhiteSpace(sortType))
        {
            supplierDtos = sortType.ToLower() switch
            {
                "asc" => supplierDtos
                    .OrderBy(s => double.Parse(((string)s.GetType()
                            .GetProperty("Distance")!
                            .GetValue(s)!)
                        .Replace(" Km", "")))
                    .ToList(),

                "desc" => supplierDtos
                    .OrderByDescending(s => double.Parse(((string)s.GetType()
                            .GetProperty("Distance")!
                            .GetValue(s)!)
                        .Replace(" Km", "")))
                    .ToList(),

                _ => supplierDtos
            };
        }

        return new OkObjectResult(new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = supplierDtos
        });
    }


    public async Task<IActionResult> GetSupplierAsync(int id, int companyId, int pageNumber = 1, int pageSize = 10,
        string? search = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var supplier = await context.Suppliers
            .Include(s => s.SupplierMaterials)
            .ThenInclude(sm => sm.Material)
            .FirstOrDefaultAsync(s => s.Id == id && (s.CompanyId == companyId || s.CompanyId == null));

        if (supplier == null)
            return new NotFoundObjectResult("Supplier not found.");

        var company = await context.Companies.FindAsync(companyId);
        if (company == null)
            return new NotFoundObjectResult("Company not found.");

        var numericDistance = LocationHelper.CalculateDistance(
            company.LocationLatitude,
            company.LocationLongitude,
            supplier.LocationLatitude,
            supplier.LocationLongitude);

        var supplierMaterials = supplier.SupplierMaterials.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            supplierMaterials = supplierMaterials
                .Where(sm => sm.Material.Name.ToLower().Contains(term));
        }

        var totalMaterials = supplierMaterials.Count();

        var pagedMaterials = supplierMaterials
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(sm => new MaterialDto
            {
                Id = sm.Material.Id,
                Name = sm.Material.Name,
                CarbonFootprint = $"{sm.Material.EmissionValue} CO2e/{sm.Material.Unit}"
            })
            .ToList();

        var result = new
        {
            Supplier = new SupplierDto
            {
                Id = supplier.Id,
                Name = supplier.Name,
                FacilityType = supplier.FacilityType,
                Sector = supplier.Sector,
                Latitude = supplier.LocationLatitude,
                Longitude = supplier.LocationLongitude,
                Distance = $"{Math.Round(numericDistance, 2)} Km",
                Location = GetLocationAsync(supplier.LocationLatitude, supplier.LocationLongitude).Result
            },
            Materials = new
            {
                TotalCount = totalMaterials,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalMaterials / (double)pageSize),
                Items = pagedMaterials
            }
        };

        return new OkObjectResult(result);
    }


    public async Task<IActionResult> CreateSupplierAsync(SupplierCreateDto dto, int companyId)
    {
        var company = await context.Companies.FindAsync(companyId);
        if (company == null)
            return new NotFoundObjectResult("Company not found.");

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

        if (dto.MaterialIds != null && dto.MaterialIds.Any())
        {
            var supplierMaterials = dto.MaterialIds.Select(materialId => new SupplierMaterial
            {
                SupplierId = supplier.Id,
                MaterialId = materialId
            });

            context.SupplierMaterials.AddRange(supplierMaterials);
            await context.SaveChangesAsync();
        }

        return new OkObjectResult(new
        {
            Message = "Supplier created successfully",
            SupplierId = supplier.Id,
        });
    }

    public async Task<IActionResult> UpdateSupplierAsync(int id, SupplierUpdateDto dto, int companyId)
    {
        var supplier = await context.Suppliers
            .Include(s => s.SupplierMaterials)
            .FirstOrDefaultAsync(s => s.Id == id && (s.CompanyId == companyId || s.CompanyId == null));

        if (supplier == null)
            return new NotFoundObjectResult("Supplier not found.");

        supplier.Name = dto.Name;
        supplier.LocationLatitude = dto.Latitude;
        supplier.LocationLongitude = dto.Longitude;
        supplier.FacilityType = string.IsNullOrWhiteSpace(dto.FacilityType) ? supplier.FacilityType : dto.FacilityType;
        supplier.Sector = string.IsNullOrWhiteSpace(dto.Sector) ? supplier.Sector : dto.Sector;

        if (dto.MaterialIds != null)
        {
            context.SupplierMaterials.RemoveRange(supplier.SupplierMaterials);

            var newLinks = dto.MaterialIds.Select(materialId => new SupplierMaterial
            {
                SupplierId = id,
                MaterialId = materialId
            }).ToList();

            await context.SupplierMaterials.AddRangeAsync(newLinks);
        }

        await context.SaveChangesAsync();

        return new OkObjectResult(new
        {
            Message = "Supplier updated successfully",
            SupplierId = supplier.Id,
        });
    }

    public async Task<IActionResult> DeleteSupplierAsync(int id, int companyId)
    {
        var supplier = await context.Suppliers.FindAsync(id);
        if (supplier == null || supplier.CompanyId != companyId)
            return new NotFoundObjectResult("Supplier not found");

        context.Suppliers.Remove(supplier);
        await context.SaveChangesAsync();

        return new OkObjectResult("Supplier deleted successfully");
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

    private async Task<string> GetLocationAsync(decimal latitude, decimal longitude)
    {
        var url =
            $"https://api.geoapify.com/v1/geocode/reverse?lat={latitude}&lon={longitude}&apiKey=0e99ae334bed4b85b7f75ff56a8d1226";
        var geoResponse = await externalApiService.GetAsync<GeoapifyResponse>(url);

        return geoResponse?.Features?.FirstOrDefault()?.Properties?.Formatted ?? "Location not available";
    }
}