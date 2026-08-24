using System.Security.Claims;
using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Identity;

namespace RestaurantMenu.WebUI.Infrastructure;

public static class OrderStatusPolicy
{
    public static IReadOnlyList<string> ResolveRoles(ClaimsPrincipal user)
    {
        return AppRoles.All.Where(user.IsInRole).ToArray();
    }

    public static bool CanChange(IEnumerable<string> roles, OrderStatus from, OrderStatus to)
    {
        var roleList = roles.ToList();
        if (roleList.Contains(AppRoles.Admin))
        {
            return true;
        }

        if (roleList.Contains(AppRoles.Mutfak))
        {
            return (from == OrderStatus.New && to == OrderStatus.Preparing)
                || (from == OrderStatus.Confirmed && to == OrderStatus.Preparing)
                || (from == OrderStatus.Preparing && to == OrderStatus.Ready);
        }

        if (roleList.Contains(AppRoles.Personel))
        {
            return (from == OrderStatus.New && (to == OrderStatus.Confirmed || to == OrderStatus.Cancelled))
                || (from == OrderStatus.Confirmed && to == OrderStatus.Cancelled)
                || (from == OrderStatus.Ready && to == OrderStatus.Served)
                || (from == OrderStatus.Served && to == OrderStatus.Completed);
        }

        return false;
    }
}
