using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RestaurantMenu.Entities.Identity;

namespace RestaurantMenu.WebUI.Infrastructure;

public class CurrentRestaurantFilter : IAsyncActionFilter
{
    private readonly ICurrentRestaurant _current;

    public CurrentRestaurantFilter(ICurrentRestaurant current)
    {
        _current = current;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var area = context.RouteData.Values["area"]?.ToString();
        if (area is not ("Admin" or "Staff" or "Kitchen"))
        {
            await next();
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();
        if (string.Equals(controller, "Users", StringComparison.OrdinalIgnoreCase)
            || string.Equals(controller, "Logs", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(controller, "Restaurant", StringComparison.OrdinalIgnoreCase)
                && action is "Index" or "Create"))
        {
            await next();
            return;
        }

        await _current.EnsureAsync();
        if (_current.Id is null)
        {
            if (context.HttpContext.User.IsInRole(AppRoles.Admin))
            {
                context.Result = new RedirectToActionResult("Create", "Restaurant", new { area = "Admin" });
                return;
            }

            context.Result = new ViewResult { ViewName = "~/Views/Shared/NoRestaurant.cshtml" };
            return;
        }

        if (context.Controller is Controller mvc)
        {
            var restaurant = await _current.GetAsync();
            mvc.ViewData["CurrentRestaurantName"] = restaurant?.Name;
        }

        await next();
    }
}
