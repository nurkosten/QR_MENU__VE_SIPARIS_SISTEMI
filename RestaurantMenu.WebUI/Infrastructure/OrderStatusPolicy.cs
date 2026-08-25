using RestaurantMenu.Entities.Enums;

namespace RestaurantMenu.WebUI.Infrastructure;

public static class OrderStatusPolicy
{
    public static bool CanKitchenChange(OrderStatus from, OrderStatus to)
    {
        return (from == OrderStatus.Confirmed && to == OrderStatus.Preparing)
            || (from == OrderStatus.Preparing && to == OrderStatus.Ready);
    }

    public static bool CanStaffChange(OrderStatus from, OrderStatus to)
    {
        return (from == OrderStatus.New && (to == OrderStatus.Confirmed || to == OrderStatus.Cancelled))
            || (from == OrderStatus.Confirmed && to == OrderStatus.Cancelled)
            || (from == OrderStatus.Ready && to == OrderStatus.Served)
            || (from == OrderStatus.Served && to == OrderStatus.Completed);
    }
}
