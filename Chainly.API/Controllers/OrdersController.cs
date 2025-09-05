using Microsoft.AspNetCore.Mvc;

namespace Chainly.Controllers;

    [ApiController]
    [Route("api/[controller]")]
public class OrdersController : ControllerBase
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
}