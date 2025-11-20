using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface ICustomerService
{
    Task<IActionResult> GetCurrentCustomerAsync(int customerId);
}