using Chainly.Application.DTOs.Users;
namespace Chainly.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class CustomersController(ICustomerService customerService) : BaseController
{
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentCustomer()
    {
        int customerId = GetUserId();

        return await customerService.GetCurrentCustomerAsync(customerId);
    }
}