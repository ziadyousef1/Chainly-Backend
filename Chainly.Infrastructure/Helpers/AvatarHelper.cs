using Chainly.Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace Chainly.Infrastructure.Helpers;

public class AvatarHelper
{
    public static string GetDefaultAvatar(IHttpContextAccessor accessor)
    {
        var request = accessor.HttpContext?.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        return $"{baseUrl}{DefaultAssets.DefaultCompanyAvatar}";
    }
}