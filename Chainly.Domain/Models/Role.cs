using Microsoft.AspNetCore.Identity;

namespace Chainly.Domain.Models;

public class Role : IdentityRole<int>
{
    public int CompanyId { get; set; }
}