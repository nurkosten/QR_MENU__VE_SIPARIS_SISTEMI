using RestaurantMenu.Entities.Enums;

namespace RestaurantMenu.Business.Orders;

public static class ServiceRequestMachine
{
    public static bool CanTransition(ServiceRequestStatus from, ServiceRequestStatus to)
    {
        return from switch
        {
            ServiceRequestStatus.Pending => to is ServiceRequestStatus.Acknowledged or ServiceRequestStatus.Completed,
            ServiceRequestStatus.Acknowledged => to == ServiceRequestStatus.Completed,
            _ => false
        };
    }
}
