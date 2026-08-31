using Hangfire.Dashboard;
using RestaurantMenu.Entities.Identity;

namespace RestaurantMenu.WebUI.Infrastructure;

public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole(AppRoles.Admin);
    }
}
