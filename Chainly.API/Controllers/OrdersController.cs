using Chainly.API.Controllers;
using Chainly.Application.DTOs.Orders;
using Chainly.Application.Services.Interfaces;
using Chainly.Domain.Constants;
using Chainly.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : BaseController
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("{code}/update")]
    public async Task<IActionResult> GetOrderUpdate(string code)
    {
        var companyId = GetCompanyId();
        var response = await _orderService.GetOrderUpdateAsync(code,companyId);
        if (!response.IsSuccess)
        {
            return NotFound(response.Message);
        }

        return Ok(response.Message);
    }

    [HttpPost("import")]
    [Authorize(Permissions.Orders.Create)]
    public async Task<IActionResult> ImportOrders(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Please select a valid Excel file.");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only Excel files (.xlsx) are supported.");
        var companyId = GetCompanyId();
        return await _orderService.ImportOrdersFromExcelAsync(file, companyId);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(int pageNumber = 1, int pageSize = 10, OrderStatus? status = null,
        string? search = null)
    {
        var companyId = GetCompanyId();
        return await _orderService.GetPagedOrdersAsync(companyId, pageNumber, pageSize, status, search);
    }

    [HttpPost("create-order-tracking")]
    public async Task<IActionResult> CreateOrderTracking([FromBody] OrderTrackingDto orderTrackingDto)
    {
        if (orderTrackingDto == null)
        {
            return BadRequest("Invalid order tracking data.");
        }

        var companyId = GetCompanyId();
        var updatedBy = GetUserId();

        return await _orderService.CreateOrderAsync(orderTrackingDto, companyId, updatedBy);
    }

    [HttpGet("{orderId}/tracking")]
    public async Task<IActionResult> GetOrderTrackingByIdAsync(int orderId)
    {
        var companyId = GetCompanyId();
        var result = await _orderService.GetOrderTrackingByIdAsync(orderId, companyId);
        return result;
    }

    [HttpPut("{orderId}")]
    [Authorize(Permissions.Orders.Update)]
    public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] UpdateOrderDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid order data.");

        var result = await _orderService.UpdateOrderAsync(orderId, dto);
        if (!result.IsSuccess)
            return BadRequest(result.Message);

        return Ok(result.Message);
    }

    [HttpPut("tracking/{trackingId}")]
    [Authorize(Permissions.Orders.Update)]
    public async Task<IActionResult> UpdateOrderTracking(int trackingId, [FromBody] UpdateOrderTrackingDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid tracking data.");

        var result = await _orderService.UpdateOrderTrackingAsync(trackingId, dto);
        if (!result.IsSuccess)
            return BadRequest(result.Message);

        return Ok(result.Message);
    }


    [HttpGet("summary")]
    public async Task<IActionResult> GetOrdersSummary()
    {
        var companyId = GetCompanyId();
        return await _orderService.GetOrdersSummaryAsync(companyId);
    }

    [HttpGet("statuses")]
    public IActionResult GetOrderStatuses()
    {
        var statuses = EnumHelper.ToDtoList<OrderStatus, EnumDto>((id, name) => new EnumDto { Id = id, Name = name });

        return new OkObjectResult(statuses);
    }
}