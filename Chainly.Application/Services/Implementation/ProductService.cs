using Chainly.Application.DTOs.Products;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class ProductService(AppDbContext context) : IProductService
{
    public async Task<IActionResult> GetAllProductsAsync(
        int companyId,
        int pageNumber = 1,
        int pageSize = 10,
        string? search = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.Products.Where(p => p.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Stock = p.Stock - context.SalesData.Count(s => s.ProductId == p.Id && s.CompanyId == companyId)
            })
            .ToListAsync();

        var response = new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = products
        };

        return new OkObjectResult(response);
    }

    public async Task<IActionResult> GetTopProductsAsync(int companyId, int topCount = 5)
    {
        if (topCount <= 0) topCount = 5;

        var topProducts = await context.SalesData
            .Where(s => s.CompanyId == companyId)
            .GroupBy(s => s.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                SalesCount = g.Count()
            })
            .OrderByDescending(x => x.SalesCount)
            .Take(topCount)
            .Join(
                context.Products.Where(p => p.CompanyId == companyId),
                s => s.ProductId,
                p => p.Id,
                (s, p) => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Stock = p.Stock - s.SalesCount
                }
            )
            .ToListAsync();

        return new OkObjectResult(topProducts);
    }
}