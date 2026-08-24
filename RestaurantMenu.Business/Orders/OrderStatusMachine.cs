using RestaurantMenu.Entities.Enums;

namespace RestaurantMenu.Business.Orders;

public static class OrderStatusMachine
{
    public static bool CanTransition(OrderStatus from, OrderStatus to)
    {
        return GetAllowedTargets(from).Contains(to);
    }

    public static IReadOnlyCollection<OrderStatus> GetAllowedTargets(OrderStatus from)
    {
        return from switch
        {
            OrderStatus.New => [OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Cancelled],
            OrderStatus.Confirmed => [OrderStatus.Preparing, OrderStatus.Cancelled],
            OrderStatus.Preparing => [OrderStatus.Ready],
            OrderStatus.Ready => [OrderStatus.Served],
            OrderStatus.Served => [OrderStatus.Completed],
            _ => []
        };
    }
}
