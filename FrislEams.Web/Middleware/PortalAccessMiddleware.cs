using FrislEams.Web.Services;

namespace FrislEams.Web.Middleware;

public sealed class PortalAccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, RoleGuard roleGuard)
    {
        var path = context.Request.Path.Value ?? "/";
        if (IsStaticAssetPath(path))
        {
            await next(context);
            return;
        }

        var role = roleGuard.GetCurrentRole(context);
        var isLoggedIn = !string.IsNullOrWhiteSpace(context.Session.GetString("UserName"));

        if (!isLoggedIn && !IsAnonymousPath(path))
        {
            var returnUrl = string.Equals(path, "/Home/Error", StringComparison.OrdinalIgnoreCase)
                ? "/"
                : path;
            context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return;
        }

        if (isLoggedIn && context.Session.GetString("MustChangePassword") == "1"
            && !path.StartsWith("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/Account/ChangePassword");
            return;
        }

        if (isLoggedIn && !PortalService.CanAccessPath(role, path))
        {
            context.Response.Redirect(PortalService.GetHomePath(role));
            return;
        }

        await next(context);
    }

    private static bool IsStaticAssetPath(string path)
        => path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/_", StringComparison.OrdinalIgnoreCase)
           || path.Contains('.', StringComparison.Ordinal);

    private static bool IsAnonymousPath(string path)
        => path.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/Home/Error", StringComparison.OrdinalIgnoreCase);
}
