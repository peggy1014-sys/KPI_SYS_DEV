using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KpiSys.Web;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles)
    {
        _roles = roles ?? Array.Empty<string>();
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        if (!userId.HasValue)
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        if (_roles.Length > 0)
        {
            var role = session.GetString(SessionKeys.UserRole);
            if (string.IsNullOrWhiteSpace(role) || !_roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("Forbidden", "Auth", null);
                return;
            }
        }

        await next();
    }
}
