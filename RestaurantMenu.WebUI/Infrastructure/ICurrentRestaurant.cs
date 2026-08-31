using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.WebUI.Infrastructure;

public interface ICurrentRestaurant
{
    const string SessionKey = "currentRestaurantId";

    int? Id { get; }

    bool CanSwitch { get; }

    void Set(int id);

    Task EnsureAsync(CancellationToken cancellationToken = default);

    Task<Restaurant?> GetAsync(CancellationToken cancellationToken = default);

    Task<RestaurantTable?> GetPreviewTableAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Restaurant>> ListAsync(CancellationToken cancellationToken = default);
}
