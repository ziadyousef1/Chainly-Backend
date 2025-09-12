using Chainly.Application.DTOs;
using Chainly.Application.DTOs.Orders;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Constants;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Chainly.Application.Services.Implementation;

public class OrderService : IOrderService
{
    private readonly IAzureOpenAIService _azureOpenAiService;
    private readonly AppDbContext _dbContext;

    public OrderService(IAzureOpenAIService azureOpenAiService, AppDbContext dbContext)
    {
        _azureOpenAiService = azureOpenAiService;
        _dbContext = dbContext;
    }
    public async Task<ServiceResponse> GetOrderUpdateAsync(int orderId)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.CompanyOrderId == orderId);
        if (order == null)
        {
            return new ServiceResponse("Order not found", false);
        }

        var orderTracking = await _dbContext.OrderTracking
            .Where(ot => ot.OrderId == orderId)
            .Select(ot => new { ot.Status, ot.Timestamp }).ToListAsync();
        var orderTrackingJson = System.Text.Json.JsonSerializer.Serialize(orderTracking);
        var message = await _azureOpenAiService.GenerateOrderUpdateMessageAsync(orderTrackingJson);
        return new ServiceResponse(message, true);

    }

    public async Task<IActionResult> ImportOrdersFromExcelAsync(IFormFile file, int companyId)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Order Import");

        var ordersList = new List<OrderImportDto>();

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
            var orderId = int.Parse(worksheet.Cells[row, 1].Text?.Trim());
            var codeText = worksheet.Cells[row, 2].Text?.Trim();
            var phoneText = worksheet.Cells[row, 3].Text?.Trim();
            var emailText = worksheet.Cells[row, 4].Text?.Trim();

            if (string.IsNullOrEmpty(codeText))
                continue;

            var order = new OrderImportDto
            {
                OrderId = orderId,
                Email = emailText,
                Phone = phoneText,
                Code = codeText

            };

            ordersList.Add(order);
        }

        if (!ordersList.Any())
        {
            return new BadRequestObjectResult("No valid order data found in the Excel file");
        }

        var orderEntities = ordersList.Select(dto => new Order
        {
            CompanyOrderId = dto.OrderId,
            CompanyId = companyId,
            Email = dto.Email,
            Phone = dto.Phone,
            Code = dto.Code
        }).ToList();

        await _dbContext.Orders.AddRangeAsync(orderEntities);
        var savedCount = await _dbContext.SaveChangesAsync();

        var result = new
        {
            TotalProcessed = ordersList.Count,
            Inserted = savedCount,
            Message = $"Successfully imported {savedCount} orders."
        };

        return new OkObjectResult(result);
    }
    public async Task<IActionResult> GetPagedOrdersAsync(int companyId, int pageNumber = 1, int pageSize = 10)
{
    if (pageNumber <= 0) pageNumber = 1;
    if (pageSize <= 0) pageSize = 10;

    var query = _dbContext.Orders
        .Where(o => o.CompanyId == companyId);

    var totalCount = await query.CountAsync();

    var orders = await query
        .OrderBy(o => o.CompanyOrderId)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .Select(o => new { OrderId = o.CompanyOrderId, o.Code })
        .ToListAsync();

    var response = new
    {
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        Items = orders
    };

    return new OkObjectResult(response);
}

    public async Task<IActionResult> GetOrderTrackingByIdAsync(int orderId,int companyId)
    {
        throw new NotImplementedException();
    }

    public async Task<IActionResult> CreateOrderAsync(OrderTrackingDto orderTrackingDto,int companyId,int UpdatedBy)
    {
        var orderTracking = new OrderTracking
        {
            OrderId = orderTrackingDto.OrderId,
            CompanyId = companyId,
            Latitude = orderTrackingDto.Latitude,
            Longitude = orderTrackingDto.Longitude,
            Status = orderTrackingDto.Status,
            Notes = orderTrackingDto.Notes,
            Timestamp = DateTime.UtcNow,
            UpdatedBy = UpdatedBy
        };
        var order = await _dbContext.Orders
            .Where(o => o.CompanyOrderId == orderTrackingDto.OrderId && o.CompanyId == companyId)
            .Select(o => new { o.Id })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return new BadRequestObjectResult("Order does not exist.");
        }
        var orderTarckingExists = _dbContext.OrderTracking.Any(ot => ot.OrderId == orderTrackingDto.OrderId && ot.CompanyId == companyId && ot.Status == orderTrackingDto.Status);
        if (orderTarckingExists)
        {
            return new BadRequestObjectResult("Order tracking with the same status already exists.");
        }
        orderTracking.OrderId = order.Id;
        await _dbContext.OrderTracking.AddAsync(orderTracking);
        await _dbContext.SaveChangesAsync();
        return new OkObjectResult("Order tracking created successfully.");
    }

    public async Task<IActionResult> GetOrdersSummaryAsync(int companyId)
    {
        var summary = await _dbContext.Orders
            .Where(o => o.CompanyId == companyId)
            .GroupBy(o => 1) 
            .Select(g => new
            {
                DeliveredOrders = g.Count(o => o.Trackings.Any(t => t.Status == OrderStatus.Delivered)),
                PendingOrders = g.Count(o => o.Trackings.Any(t => t.Status == OrderStatus.Pending)),
                CancelledOrders = g.Count(o => o.Trackings.Any(t => t.Status == OrderStatus.Cancelled)),
                TotalPlacedOrders = g.Count()
            })
            .FirstOrDefaultAsync();

        return new OkObjectResult(summary);
    }
}