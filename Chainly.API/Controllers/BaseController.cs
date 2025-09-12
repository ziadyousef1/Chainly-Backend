using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Chainly.API.Controllers;

public class BaseController : ControllerBase
{
    protected int GetCompanyId()
    {
        var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;
        if (companyIdClaim == null)
            throw new UnauthorizedAccessException("CompanyId not found in token.");

        return int.Parse(companyIdClaim);
    }
    
    protected int GetUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            throw new UnauthorizedAccessException("UserId not found in token.");

        return int.Parse(userIdClaim);
    }
}