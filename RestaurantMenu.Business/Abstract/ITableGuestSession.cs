namespace RestaurantMenu.Business.Abstract;

public interface ITableGuestSession
{
    Task<bool> TryBindAsync(int tableId, string guestToken, CancellationToken cancellationToken = default);

    Task ReleaseAsync(int tableId, CancellationToken cancellationToken = default);

    Task<bool> IsOccupiedByOtherAsync(int tableId, string? guestToken, CancellationToken cancellationToken = default);
}
