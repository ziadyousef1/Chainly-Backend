using Chainly.Application.DTOs.ProductionLines;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class ProductionLineService(AppDbContext context) : IProductionLineService
{
    public async Task<IActionResult> GetAllAsync(int companyId, int pageNumber = 1, int pageSize = 10)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.ProductionLines
            .Where(l => l.CompanyId == companyId);

        var totalCount = await query.CountAsync();

        var lines = await query
            .OrderBy(l => l.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new ProductionLineDto
            {
                Id = l.Id,
                LineName = l.LineName,
                Description = l.Description
            })
            .ToListAsync();

        var result = new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = lines
        };

        return new OkObjectResult(result);
    }


    public async Task<IActionResult> GetAsync(int id, int companyId)
    {
        var line = await context.ProductionLines
            .FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == companyId);

        if (line is null)
            return new NotFoundObjectResult(new { message = "Production line not found" });

        var dto = new ProductionLineDto
        {
            Id = line.Id,
            LineName = line.LineName,
            Description = line.Description
        };

        return new OkObjectResult(dto);
    }

    public async Task<IActionResult> CreateAsync(ProductionLineCreateDto dto, int companyId)
    {
        if (string.IsNullOrWhiteSpace(dto.LineName))
            return new BadRequestObjectResult(new { message = "Line name is required" });

        var exists = await context.ProductionLines
            .AnyAsync(l => l.LineName == dto.LineName && l.CompanyId == companyId);

        if (exists)
            return new BadRequestObjectResult(new { message = "Line already exists" });

        var line = new ProductionLine
        {
            LineName = dto.LineName.Trim(),
            Description = dto.Description,
            CompanyId = companyId
        };

        context.ProductionLines.Add(line);
        await context.SaveChangesAsync();

        return new OkObjectResult(new { message = "Production line created successfully", line.Id, line.LineName });
    }


    public async Task<IActionResult> UpdateAsync(int id, int companyId, ProductionLineUpdateDto dto)
    {
        var line = await context.ProductionLines
            .FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == companyId);

        if (line is null)
            return new NotFoundObjectResult(new { message = "Production line not found" });

        var exists = await context.ProductionLines
            .AnyAsync(l => l.CompanyId == companyId
                           && l.LineName.ToLower() == dto.LineName.Trim().ToLower()
                           && l.Id != id);

        if (exists)
            return new BadRequestObjectResult(new { message = "A production line with this name already exists" });

        line.LineName = dto.LineName.Trim();
        line.Description = dto.Description;

        await context.SaveChangesAsync();

        return new OkObjectResult(new { message = "Production line updated successfully" });
    }

    public async Task<IActionResult> DeleteAsync(int id, int companyId)
    {
        var line = await context.ProductionLines
            .FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == companyId);

        if (line is null)
            return new NotFoundObjectResult(new { message = "Production line not found" });

        context.ProductionLines.Remove(line);
        await context.SaveChangesAsync();

        return new OkObjectResult(new { message = "Production line deleted successfully" });
    }
}