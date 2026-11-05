using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TitoRegistry.Api.Filters;

/// <summary>
/// Filtro de autorización que exige el cambio obligatorio de contraseña temporal antes de permitir
/// el acceso a endpoints operativos o administrativos del sistema.
/// </summary>
public class MustChangePasswordFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> AllowedPathPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/change-password",
        "/api/auth/logout",
        "/api/auth/me",
        "/api/status"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var mustChange = user.FindFirst("mustChangePassword")?.Value;
            if (string.Equals(mustChange, "true", StringComparison.OrdinalIgnoreCase))
            {
                var requestPath = context.HttpContext.Request.Path.Value ?? string.Empty;
                bool isAllowed = AllowedPathPrefixes.Any(p => requestPath.StartsWith(p, StringComparison.OrdinalIgnoreCase));

                if (!isAllowed)
                {
                    context.Result = new ObjectResult(new
                    {
                        message = "Debes cambiar tu contraseña temporal antes de acceder a las funciones del sistema.",
                        mustChangePassword = true
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }
            }
        }

        await next();
    }
}
