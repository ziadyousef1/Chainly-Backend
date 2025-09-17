using Chainly.Application.DTOs.Materials;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class MaterialService(AppDbContext context) : IMaterialService
{
    public async Task<IActionResult> GetAllMaterialsAsync(
        int companyId,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortOrder = null,
        string? search = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.Materials.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(m => m.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(sortOrder))
        {
            query = sortOrder.ToLower() switch
            {
                "desc" => query.OrderByDescending(m => m.EmissionValue),
                "asc" => query.OrderBy(m => m.EmissionValue),
                _ => query.OrderBy(m => m.Name)
            };
        }

        var materials = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MaterialDto
            {
                Id = m.Id,
                Name = m.Name,
                CarbonFootprint = $"{m.EmissionValue} CO2e/{m.Unit}"
            })
            .ToListAsync();

        var result = new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = materials
        };

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetMaterialByIdAsync(
        int id,
        int companyId,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortOrder = null,
        string? search = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var material = await context.Materials
            .Include(m => m.SupplierMaterials)
            .ThenInclude(sm => sm.Supplier)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (material is null)
            return new NotFoundObjectResult(new { message = "Material not found" });

        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is null)
            return new BadRequestObjectResult(new { message = "Company not found" });

        var suppliersList = material.SupplierMaterials
            .Where(sm => sm.Supplier.CompanyId == companyId || sm.Supplier.CompanyId == null)
            .Select(sm =>
            {
                var supplier = sm.Supplier;
                var distance = LocationHelper.CalculateDistance(
                    company.LocationLatitude, company.LocationLongitude,
                    supplier.LocationLatitude, supplier.LocationLongitude);

                var footprintValue = distance * material.EmissionValue;

                return new
                {
                    supplier.Id,
                    supplier.Name,
                    FootprintValue = Math.Round(footprintValue, 2)
                };
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            suppliersList = suppliersList
                .Where(s => s.Name.ToLower().Contains(term))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(sortOrder))
        {
            suppliersList = sortOrder.ToLower() switch
            {
                "desc" => suppliersList.OrderByDescending(s => s.FootprintValue).ToList(),
                "asc" => suppliersList.OrderBy(s => s.FootprintValue).ToList(),
                _ => suppliersList
            };
        }

        var totalSuppliers = suppliersList.Count;

        var pagedSuppliers = suppliersList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SupplierCarbonFootprintDto
            {
                Id = s.Id,
                Name = s.Name,
                CarbonFootprint = $"{s.FootprintValue} CO2e/{material.Unit}"
            })
            .ToList();

        var dto = new MaterialDetailsDto
        {
            Name = material.Name,
            CarbonFootprint = $"{material.EmissionValue} CO2e/{material.Unit}",
            Suppliers = pagedSuppliers
        };

        return new OkObjectResult(new
        {
            TotalCount = totalSuppliers,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalSuppliers / (double)pageSize),
            Item = dto
        });
    }
}