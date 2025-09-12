using Chainly.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IOrderService
{
    Task<ServiceResponse> GetOrderUpdateAsync(int orderId);
    Task<IActionResult> ImportOrdersFromExcelAsync(IFormFile file, int companyId);
    Task<IActionResult> GetPagedOrdersAsync(int companyId, int pageNumber = 1, int pageSize = 10);
    Task<IActionResult> GetOrderTrackingByIdAsync(int orderId,int companyId);
    Task<IActionResult> CreateOrderAsync(OrderTrackingDto orderTrackingDto,int companyId,int UpdatedBy);
    Task<IActionResult> GetOrdersSummaryAsync(int companyId);
}