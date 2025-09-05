namespace Chainly.Controllers;

public class BaseController : ControllerBase
{
    protected int GetCompanyId()
    {
        var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;
        if (companyIdClaim == null)
            throw new UnauthorizedAccessException("CompanyId not found in token.");

        return int.Parse(companyIdClaim);
    }
}