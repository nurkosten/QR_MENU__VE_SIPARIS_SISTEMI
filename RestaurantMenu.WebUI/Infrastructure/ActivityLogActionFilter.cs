using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RestaurantMenu.Business.Dtos;

namespace RestaurantMenu.WebUI.Infrastructure;

public sealed class ActivityLogActionFilter : IAsyncActionFilter
{
    private readonly IActivityLogQueue _queue;

    public ActivityLogActionFilter(IActivityLogQueue queue)
    {
        _queue = queue;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        var http = context.HttpContext;
        var method = http.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            return;
        }

        var path = http.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        if (string.Equals(controller, "Logs", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var area = context.RouteData.Values["area"]?.ToString() ?? string.Empty;
        var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
        var status = executed.Exception is not null
            ? 500
            : http.Response.StatusCode is >= 200 and < 600 ? http.Response.StatusCode : StatusFromResult(executed.Result);

        _queue.Enqueue(new ActivityLogEntry
        {
            Level = executed.Exception is not null || status >= 500 ? "Error" : status >= 400 ? "Warning" : "Info",
            Category = Category(area, controller),
            Message = $"{method} {path} · {controller}/{action}",
            UserName = http.User.Identity?.Name,
            Path = path,
            HttpMethod = method,
            StatusCode = status,
            RestaurantId = http.Session.GetInt32(ICurrentRestaurant.SessionKey)
        });
    }

    private static int StatusFromResult(IActionResult? result) => result switch
    {
        ForbidResult => 403,
        UnauthorizedResult => 401,
        NotFoundResult => 404,
        BadRequestResult => 400,
        StatusCodeResult code => code.StatusCode,
        RedirectResult or RedirectToActionResult or RedirectToRouteResult or LocalRedirectResult => 302,
        _ => 200
    };

    private static string Category(string area, string controller)
    {
        if (string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return "Yönetim";
        }

        if (string.Equals(area, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            return "Personel";
        }

        if (string.Equals(area, "Kitchen", StringComparison.OrdinalIgnoreCase))
        {
            return "Mutfak";
        }

        return controller.ToLowerInvariant() switch
        {
            "account" => "Hesap",
            "cart" => "Sipariş",
            "service" => "Servis",
            "menu" => "Menü",
            _ => "Genel"
        };
    }
}
