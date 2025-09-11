using Chainly.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Chainly.Infrastructure.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
