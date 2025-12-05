using Chainly.Application.DTOs;
using Chainly.Application.DTOs.Orders;
using Chainly.Application.DTOs.Orders.Customer;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Constants;
using Chainly.Domain.Models;
using Chainly.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Chainly.Application.Services.Implementation;

public class OrderService(
    IDeepSeekService deepSeekService,
    AppDbContext dbContext,
    IExternalApiService externalApiService)
    : IOrderService
{
    public async Task<ServiceResponse> GetOrderUpdateAsync(string code, int companyId)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Code == code);
        if (order == null)
        {
            return new ServiceResponse("Order not found", false);
        }

        var orderTracking = await dbContext.OrderTracking
            .Where(o => o.OrderId == order.Id && o.CompanyId == companyId)
            .Select(ot => new { ot.Status, ot.Timestamp, ot.Notes })
            .ToListAsync();
        var orderTrackingJson = System.Text.Json.JsonSerializer.Serialize(orderTracking);
        var message = await deepSeekService.GenerateOrderUpdateMessageAsync(orderTrackingJson);
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

        await dbContext.Orders.AddRangeAsync(orderEntities);
        var savedCount = await dbContext.SaveChangesAsync();

        var result = new
        {
            TotalProcessed = ordersList.Count,
            Inserted = savedCount,
            Message = $"Successfully imported {savedCount} orders."
        };

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetPagedOrdersAsync(int companyId, int pageNumber = 1, int pageSize = 10,
        OrderStatus? status = null
        , string? searchTerm = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = dbContext.Orders
            .Where(o => o.CompanyId == companyId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Trackings.Any(t => t.Status == status.Value));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(o =>
                o.CompanyOrderId.ToString().Contains(searchTerm) ||
                o.Code.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderBy(o => o.CompanyOrderId)
            .Include(o => o.Trackings)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                Id = o.Id,
                OrderId = o.CompanyOrderId,
                o.Code,
                LastStatus = o.Trackings
                    .OrderByDescending(t => t.Timestamp)
                    .Select(t => t.Status.ToString())
                    .FirstOrDefault() ?? OrderStatus.Pending.ToString(),
            })
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

    public async Task<IActionResult> GetCustomerPagedOrdersAsync(int customerId, int pageNumber = 1, int pageSize = 10,
        OrderStatus? status = null,
        string? searchTerm = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var customer = dbContext.Users.FirstOrDefault(u => u.Id == customerId);

        var query = dbContext.Orders
            .Where(o => o.Phone == customer.PhoneNumber || o.Email == customer.Email);

        if (status.HasValue)
        {
            query = query.Where(o => o.Trackings.Any(t => t.Status == status.Value));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(o =>
                o.Code.Contains(searchTerm) ||
                o.Company.Name.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderBy(o => o.CompanyOrderId)
            .Include(o => o.Trackings)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                Id = o.Id,
                companyName = o.Company.Name,
                code = o.Code,
                lastStatus = o.Trackings
                    .OrderByDescending(t => t.Timestamp)
                    .Select(t => t.Status.ToString())
                    .FirstOrDefault() ?? OrderStatus.Pending.ToString(),
            })
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

    public async Task<IActionResult> GetOrderTrackingByIdAsync(int orderId, int companyId)
    {
        var order = await dbContext.Orders
            .Include(o => o.Trackings)
            .ThenInclude(t => t.UpdatedByNavigation)
            .Where(o => o.CompanyOrderId == orderId && o.CompanyId == companyId)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                CompanyOrderId = o.CompanyOrderId,
                Code = o.Code,
                Email = o.Email,
                Phone = o.Phone,
                Trackings = o.Trackings
                    .OrderByDescending(t => t.Timestamp)
                    .Select(t => new OrderTrackingDetailsDto()
                    {
                        Id = t.Id,
                        Status = t.Status.ToString(),
                        Notes = t.Notes,
                        Timestamp = t.Timestamp.ToAppFormat(),
                        Latitude = t.Latitude,
                        Longitude = t.Longitude,
                        UpdatedBy = new UpdatedByDto
                        {
                            Id = t.UpdatedByNavigation.Id,
                            FullName = t.UpdatedByNavigation.FullName
                        }
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return new NotFoundObjectResult("Order not found.");
        }

        foreach (var tracking in order.Trackings)
        {
            if (tracking.Latitude != 0 && tracking.Longitude != 0)
            {
                tracking.Location = await GetLocationAsync(tracking.Latitude, tracking.Longitude);
            }
            else
            {
                tracking.Location = "Location not available";
            }
        }


        return new OkObjectResult(order);
    }

    public async Task<IActionResult> GetCustomerOrderTrackingByIdAsync(int orderId, int customerId)
    {
        var customer = dbContext.Users.FirstOrDefault(u => u.Id == customerId);

        var order = await dbContext.Orders
            .Include(o => o.Trackings)
            .Where(o => (o.Phone == customer.PhoneNumber || o.Email == customer.Email) && o.Id == orderId)
            .Select(o => new CustomerOrderDto()
            {
                CompanyName = o.Company.Name,
                Code = o.Code,
                Trackings = o.Trackings
                    .OrderByDescending(t => t.Timestamp)
                    .Select(t => new CustomerOrderTrackingDetailsDto()
                    {
                        Status = t.Status.ToString(),
                        Timestamp = t.Timestamp.ToAppFormat(),
                        Latitude = t.Latitude,
                        Longitude = t.Longitude,
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return new NotFoundObjectResult("Order not found.");
        }

        foreach (var tracking in order.Trackings)
        {
            if (tracking.Latitude != 0 && tracking.Longitude != 0)
            {
                tracking.Location = await GetLocationAsync(tracking.Latitude, tracking.Longitude);
            }
            else
            {
                tracking.Location = "Location not available";
            }
        }


        return new OkObjectResult(order);
    }

    public async Task<ServiceResponse> UpdateOrderAsync(int orderId, UpdateOrderDto dto)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.CompanyOrderId == orderId);
        if (order == null)
            return new ServiceResponse("Order not found.", false);

        order.CompanyOrderId = dto.CompanyOrderId;
        order.Code = dto.Code;
        order.Phone = dto.Phone;
        order.Email = dto.Email;

        await dbContext.SaveChangesAsync();
        return new ServiceResponse("Order updated successfully.", true);
    }

    public async Task<ServiceResponse> UpdateOrderTrackingAsync(int trackingId, UpdateOrderTrackingDto dto)
    {
        var tracking = await dbContext.OrderTracking.FindAsync(trackingId);
        if (tracking == null)
            return new ServiceResponse("Order tracking not found.", false);

        tracking.Latitude = dto.Latitude;
        tracking.Longitude = dto.Longitude;
        tracking.Status = dto.Status;
        tracking.Notes = dto.Notes;
        tracking.Timestamp = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return new ServiceResponse("Order tracking updated successfully.", true);
    }


    private async Task<string> GetLocationAsync(decimal latitude, decimal longitude)
    {
        var url =
            $"https://api.geoapify.com/v1/geocode/reverse?lat={latitude}&lon={longitude}&apiKey=0e99ae334bed4b85b7f75ff56a8d1226";
        var geoResponse = await externalApiService.GetAsync<GeoapifyResponse>(url);

        return geoResponse?.Features?.FirstOrDefault()?.Properties?.Formatted ?? "Location not available";
    }

    public async Task<IActionResult> CreateOrderAsync(OrderTrackingDto orderTrackingDto, int companyId, int UpdatedBy)
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
        var order = await dbContext.Orders
            .Where(o => o.CompanyOrderId == orderTrackingDto.OrderId && o.CompanyId == companyId)
            .Select(o => new { o.Id })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return new BadRequestObjectResult("Order does not exist.");
        }

        var orderTarckingExists = dbContext.OrderTracking.Any(ot =>
            ot.OrderId == orderTrackingDto.OrderId && ot.CompanyId == companyId &&
            ot.Status == orderTrackingDto.Status);
        if (orderTarckingExists)
        {
            return new BadRequestObjectResult("Order tracking with the same status already exists.");
        }

        orderTracking.OrderId = order.Id;
        await dbContext.OrderTracking.AddAsync(orderTracking);
        await dbContext.SaveChangesAsync();
        return new OkObjectResult("Order tracking created successfully.");
    }

    public async Task<IActionResult> GetOrdersSummaryAsync(int companyId)
    {
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = currentMonthStart.AddMonths(-1);

        var trackings = await dbContext.OrderTracking
            .Where(t => t.CompanyId == companyId && t.Timestamp >= lastMonthStart)
            .Select(t => new { t.OrderId, t.Status, t.Timestamp })
            .ToListAsync();

        var current = trackings.Where(t => t.Timestamp >= currentMonthStart).ToList();
        var last = trackings.Where(t => t.Timestamp < currentMonthStart && t.Timestamp > lastMonthStart).ToList();

        int CountByLastStatus(IEnumerable<dynamic> list, OrderStatus status)
        {
            return list
                .GroupBy(t => (int)t.OrderId)
                .Select(g => g.OrderByDescending(t => (DateTime)t.Timestamp).First())
                .Count(t => (OrderStatus)t.Status == status);
        }

        int CountTotal(IEnumerable<dynamic> list)
        {
            return list.Select(t => (int)t.OrderId).Distinct().Count();
        }

        var summary = new
        {
            DeliveredOrders = CountByLastStatus(trackings, OrderStatus.Delivered),
            DeliveredOrdersChange = CalculateChange(
                CountByLastStatus(last, OrderStatus.Delivered),
                CountByLastStatus(current, OrderStatus.Delivered)),
            
            PendingOrders = CountByLastStatus(trackings, OrderStatus.Pending),
            PendingOrdersChange = CalculateChange(
                CountByLastStatus(last, OrderStatus.Pending),
                CountByLastStatus(current, OrderStatus.Pending)),

            CancelledOrders = CountByLastStatus(trackings, OrderStatus.Cancelled),
            CancelledOrdersChange = CalculateChange(
                CountByLastStatus(last, OrderStatus.Cancelled),
                CountByLastStatus(current, OrderStatus.Cancelled)),

            TotalPlacedOrders = CountTotal(trackings),
            TotalPlacedOrdersChange = CalculateChange(CountTotal(last), CountTotal(current))
        };

        return new OkObjectResult(summary);
    }


    public async Task<IActionResult> GetOrdersMonthlyStatsAsync(int companyId, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;

        var orders = await dbContext.Orders
            .Where(o => o.CompanyId == companyId)
            .Select(o => new
            {
                OrderId = o.Id,
                CreatedAt = o.Trackings
                    .OrderBy(t => t.Timestamp)
                    .Select(t => t.Timestamp)
                    .FirstOrDefault()
            })
            .Where(o => o.CreatedAt != default)
            .ToListAsync();

        if (orders.Count == 0)
        {
            return new OkObjectResult(new
            {
                Message = "No orders found",
                Monthly = new int[12],
                Years = Array.Empty<int>()
            });
        }

        var availableYears = orders
            .GroupBy(o => o.CreatedAt.Year)
            .Select(g => g.Key)
            .OrderBy(y => y)
            .ToList();

        var monthlyCounts = orders
            .Where(o => o.CreatedAt.Year == targetYear)
            .GroupBy(o => o.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToList();

        var monthlyArray = Enumerable.Range(1, 12)
            .Select(m => monthlyCounts.FirstOrDefault(x => x.Month == m)?.Count ?? 0)
            .ToArray();

        var result = new
        {
            Year = targetYear,
            Monthly = monthlyArray,
            Years = availableYears
        };

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> DeleteOrderAsync(int orderId, int companyId)
    {
        var order = await dbContext.Orders
            .Include(o => o.Trackings)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CompanyId == companyId);

        if (order == null)
        {
            return new NotFoundObjectResult("Order not found.");
        }

        if (order.Trackings.Count != 0)
        {
            dbContext.OrderTracking.RemoveRange(order.Trackings);
        }

        dbContext.Orders.Remove(order);

        await dbContext.SaveChangesAsync();

        return new ObjectResult("Order deleted successfully.");
    }

    private double CalculateChange(int lastMonth, int currentMonth)
    {
        return lastMonth == 0 ? 0 : Math.Round(((currentMonth - lastMonth) / (double)lastMonth) * 100, 1);
    }
}