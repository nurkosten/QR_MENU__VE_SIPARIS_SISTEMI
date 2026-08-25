using RestaurantMenu.Business.Common;
using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.Business.Abstract;

public interface IServiceRequestService
{
    Task<ServiceResult<ServiceRequest>> CreateAsync(int tableId, ServiceRequestType type, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequest>> GetOpenAsync(int restaurantId, CancellationToken cancellationToken = default);

    Task<ServiceResult> ChangeStatusAsync(int id, ServiceRequestStatus status, string userId, int restaurantId, CancellationToken cancellationToken = default);
}
