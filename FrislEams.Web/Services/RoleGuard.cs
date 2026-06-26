using FrislEams.Web.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Services;

public class RoleGuard
{
    public bool HasAnyRole(ControllerBase controller, params string[] allowed)
    {
        // Check session first (set by login page)
        var role = controller.HttpContext.Session.GetString("UserRole")
            ?? controller.HttpContext.Request.Headers["X-Role"].FirstOrDefault()
            ?? controller.HttpContext.Request.Query["role"].FirstOrDefault()
            ?? RoleName.Backoffice; // Default: allow Backoffice for demo

        return allowed.Any(a => string.Equals(a, role, StringComparison.OrdinalIgnoreCase));
    }

    public string GetCurrentRole(Microsoft.AspNetCore.Http.HttpContext ctx)
    {
        return ctx.Session.GetString("UserRole") ?? RoleName.Backoffice;
    }
}
