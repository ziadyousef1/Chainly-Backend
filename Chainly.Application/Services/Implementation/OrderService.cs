using Chainly.Application.DTOs;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class OrderService:IOrderService
{
    private readonly IAzureOpenAIService _azureOpenAiService;
    private readonly AppDbContext _dbContext;

    public OrderService(IAzureOpenAIService azureOpenAiService ,AppDbContext dbContext)
    {
        _azureOpenAiService = azureOpenAiService;
        _dbContext = dbContext;
    }
    public async Task<ServiceResponse> GetOrderUpdateAsync(int orderId)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null)
        {
            return new ServiceResponse("Order not found", false);
        }

        var orderTracking = await  _dbContext.OrderTracking
            .Where(ot => ot.OrderId == orderId)
            .Select(ot => new { ot.Status, ot.Timestamp }).ToListAsync();
        var orderTrackingJson = System.Text.Json.JsonSerializer.Serialize(orderTracking);
        var message = await _azureOpenAiService.GenerateOrderUpdateMessageAsync(orderTrackingJson);
        return new ServiceResponse(message, true);

    }
}