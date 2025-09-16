using Chainly.API.Controllers;
using Chainly.Application.Services.Interfaces;
using Chainly.Domain.Constants;
using Chainly.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : BaseController
{
    [HttpGet("{orderId}/update")]
    [Authorize(Permissions.Orders.View)]
    public async Task<IActionResult> GetOrderUpdate(int orderId)
    {
        var response = await orderService.GetOrderUpdateAsync(orderId);
        if (!response.IsSuccess)
        {
            return NotFound(response.Message);
        }

        return Ok(response.Message);
    }

    [HttpPost("import")]
    [Authorize(Permissions.Orders.Import)]
    public async Task<IActionResult> ImportOrders(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Please select a valid Excel file.");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only Excel files (.xlsx) are supported.");
        var companyId = GetCompanyId();
        return await orderService.ImportOrdersFromExcelAsync(file, companyId);
    }

    [HttpGet]
    [Authorize(Permissions.Orders.View)]
    public async Task<IActionResult> GetOrders(int pageNumber = 1, int pageSize = 10)
    {
        var companyId = GetCompanyId();
        return await orderService.GetPagedOrdersAsync(companyId, pageNumber, pageSize);
    }

    [HttpPost("create-order-tracking")]
    [Authorize(Permissions.Orders.Create)]
    public async Task<IActionResult> CreateOrderTracking([FromBody] OrderTrackingDto orderTrackingDto)
    {
        if (orderTrackingDto == null)
        {
            return BadRequest("Invalid order tracking data.");
        }
        [HttpGet("{orderId}/tracking")]
        public async Task<IActionResult> GetOrderTrackingByIdAsync(int orderId)
        {
            var companyId = GetCompanyId();
            var result = await _orderService.GetOrderTrackingByIdAsync(orderId, companyId);
            return result;
        }

        var companyId = GetCompanyId();
        var updatedBy = GetUserId();

        return await orderService.CreateOrderAsync(orderTrackingDto, companyId, updatedBy);
    }

    [HttpGet("summary")]
    [Authorize(Permissions.Orders.View)]
    public async Task<IActionResult> GetOrdersSummary()
    {
        var companyId = GetCompanyId();
        return await orderService.GetOrdersSummaryAsync(companyId);
    }

    [HttpGet("statuses")]
    public IActionResult GetOrderStatuses()
    {
        var statuses = EnumHelper.ToDtoList<OrderStatus, EnumDto>((id, name) => new EnumDto { Id = id, Name = name });

        return new OkObjectResult(statuses);
    }
}