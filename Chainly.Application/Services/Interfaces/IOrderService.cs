using Chainly.Application.DTOs;

namespace Chainly.Application.Services.Interfaces;

public interface IOrderService
{
    Task<ServiceResponse> GetOrderUpdateAsync(int orderId);

}