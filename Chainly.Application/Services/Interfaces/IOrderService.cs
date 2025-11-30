using Chainly.Application.DTOs;
using Chainly.Application.DTOs.Orders;
using Chainly.Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IOrderService
{
    Task<ServiceResponse> GetOrderUpdateAsync(string code, int userId);
    Task<IActionResult> ImportOrdersFromExcelAsync(IFormFile file, int companyId);

    Task<IActionResult> GetPagedOrdersAsync(int companyId, int pageNumber = 1, int pageSize = 10,
        OrderStatus? status = null, string? searchTerm = null);

    Task<IActionResult> GetCustomerPagedOrdersAsync(int customerId, int pageNumber = 1, int pageSize = 10,
        OrderStatus? status = null, string? searchTerm = null);

    Task<IActionResult> GetOrderTrackingByIdAsync(int orderId, int companyId);
    Task<IActionResult> GetCustomerOrderTrackingByIdAsync(int orderId, int customerId);
    Task<IActionResult> CreateOrderAsync(OrderTrackingDto orderTrackingDto, int companyId, int UpdatedBy);
    Task<ServiceResponse> UpdateOrderAsync(int orderId, UpdateOrderDto dto);
    Task<ServiceResponse> UpdateOrderTrackingAsync(int trackingId, UpdateOrderTrackingDto dto);
    Task<IActionResult> GetOrdersSummaryAsync(int companyId);
}