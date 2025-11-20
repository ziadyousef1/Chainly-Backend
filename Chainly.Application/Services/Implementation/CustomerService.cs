using Chainly.Application.DTOs.Customer;
using Chainly.Application.DTOs.Users;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class CustomerService(AppDbContext context, IHttpContextAccessor accessor) : ICustomerService
{
    public async Task<IActionResult> GetCurrentCustomerAsync(int customerId)
    {
        var customer = await context.Users
            .FirstOrDefaultAsync(u => u.Id == customerId);

        if (customer == null)
            return new NotFoundObjectResult("Customer not found");

        var customerProfile = new CustomerProfileDto
        {
            ProfilePicture = customer.ProfilePicture ?? AvatarHelper.GetDefaultAvatar(accessor),
            Name = customer.FullName,
            Email = customer.Email,
            Phone = customer.PhoneNumber
        };

        return new OkObjectResult(customerProfile);
    }
}