using RestaurantMenu.Business.Dtos;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.Business.Abstract;

public interface IActivityLogService
{
    Task AddAsync(ActivityLogEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityLog>> ListRecentAsync(int take = 400, string? level = null, CancellationToken cancellationToken = default);

    Task DeleteOlderThanDaysAsync(int days, CancellationToken cancellationToken = default);
}
