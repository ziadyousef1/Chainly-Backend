using Chainly.Application.DTOs.Products;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

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

    public async Task<IActionResult> ImportProductsFromExcelAsync(IFormFile file, int companyId)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Product Import");
        
        var productsList = new List<ProductImportDto>();

        using var stream = file.OpenReadStream();
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        
        if (worksheet == null)
        {
            return new BadRequestObjectResult("No worksheet found in the Excel file");
        }

        var rowCount = worksheet.Dimension?.Rows ?? 0;
        
        if (rowCount <= 1)
        {
            return new BadRequestObjectResult("Excel file is empty or has no data rows");
        }

        for (int row = 2; row <= rowCount; row++)
        {
            var idText = worksheet.Cells[row, 1].Text?.Trim();
            var nameText = worksheet.Cells[row, 2].Text?.Trim();
            var quantityText = worksheet.Cells[row, 3].Text?.Trim();

            if (string.IsNullOrEmpty(idText) && string.IsNullOrEmpty(nameText))
                continue;

            if (!int.TryParse(idText, out var id) ||
                string.IsNullOrEmpty(nameText) ||
                !int.TryParse(quantityText, out var quantity))
            {
                return new BadRequestObjectResult($"Invalid data format in row {row}. Please check Id, Name, and quantity columns.");
            }

            var product = new ProductImportDto
            {
                Id = id,
                Name = nameText,
                Quantity = quantity,
                CompanyId = companyId 
            };

            productsList.Add(product);
        }

        if (!productsList.Any())
        {
            return new BadRequestObjectResult("No valid product data found in the Excel file");
        }

        var productEntities = productsList.Select(dto => new Product
        {
            Id = dto.Id,
            Name = dto.Name,
            Stock = dto.Quantity, 
            CompanyId = companyId 
        }).ToList();

        var existingProducts = await context.Products
            .Where(p => p.CompanyId == companyId && productsList.Select(pl => pl.Id).Contains(p.Id))
            .ToListAsync();

        var existingProductIds = existingProducts.Select(p => p.Id).ToList();
        var productsToUpdate = productEntities.Where(p => existingProductIds.Contains(p.Id)).ToList();
        var productsToInsert = productEntities.Where(p => !existingProductIds.Contains(p.Id)).ToList();

        foreach (var productToUpdate in productsToUpdate)
        {
            var existingProduct = existingProducts.First(p => p.Id == productToUpdate.Id);
            existingProduct.Name = productToUpdate.Name;
            existingProduct.Stock = productToUpdate.Stock;
        }

        if (productsToInsert.Any())
        {
            await context.Products.AddRangeAsync(productsToInsert);
        }

        var savedCount = await context.SaveChangesAsync();

        var result = new
        {
            TotalProcessed = productsList.Count,
            Updated = productsToUpdate.Count,
            Inserted = productsToInsert.Count,
            Message = $"Successfully processed {productsList.Count} products. {productsToUpdate.Count} updated, {productsToInsert.Count} inserted."
        };

        return new OkObjectResult(result);
    }
}