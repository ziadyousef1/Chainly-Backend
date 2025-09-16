using Chainly.API.Controllers;
using Chainly.Application.Services.Interfaces;
using Chainly.Domain.Constants;
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

    [HttpGet("{orderId}/update")]
    public async Task<IActionResult> GetOrderUpdate(int orderId)
    {
        var response = await _orderService.GetOrderUpdateAsync(orderId);
        if (!response.IsSuccess)
        {
            return NotFound(response.Message);
        }   
        return Ok(response.Message);
    }

    [HttpPost("import")]
   // [Authorize(Permissions.Orders.Create)]
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
        public async Task<IActionResult> GetOrders(int pageNumber = 1, int pageSize = 10)
        {
            var companyId = GetCompanyId();
            return await _orderService.GetPagedOrdersAsync(companyId, pageNumber, pageSize);
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

        [HttpGet("summary")]
        public async Task<IActionResult> GetOrdersSummary()
        {
            var companyId = GetCompanyId();
            return await _orderService.GetOrdersSummaryAsync(companyId);
        }
}