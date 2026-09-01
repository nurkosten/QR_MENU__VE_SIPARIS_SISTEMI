using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.DataAccess.Context;

namespace RestaurantMenu.Business.Concrete;

public class TableGuestSessionManager : ITableGuestSession
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(4);

    private readonly AppDbContext _db;

    public TableGuestSessionManager(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryBindAsync(int tableId, string guestToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(guestToken))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var hash = Hash(guestToken);
        var expires = now.Add(Lifetime);

        if (!string.Equals(_db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            var updated = await _db.RestaurantTables
                .Where(t => t.Id == tableId && (
                    t.GuestSessionHash == null
                    || t.GuestSessionExpiresAt == null
                    || t.GuestSessionExpiresAt < now
                    || t.GuestSessionHash == hash))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.GuestSessionHash, hash)
                    .SetProperty(t => t.GuestSessionExpiresAt, expires)
                    .SetProperty(t => t.UpdatedAt, now), cancellationToken);
            return updated == 1;
        }

        var table = await _db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == tableId, cancellationToken);
        if (table is null)
        {
            return false;
        }

        var occupied = !string.IsNullOrEmpty(table.GuestSessionHash)
            && table.GuestSessionExpiresAt is { } until
            && until >= now
            && table.GuestSessionHash != hash;
        if (occupied)
        {
            return false;
        }

        table.GuestSessionHash = hash;
        table.GuestSessionExpiresAt = expires;
        table.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task ReleaseAsync(int tableId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        if (!string.Equals(_db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            await _db.RestaurantTables
                .Where(t => t.Id == tableId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.GuestSessionHash, (string?)null)
                    .SetProperty(t => t.GuestSessionExpiresAt, (DateTime?)null)
                    .SetProperty(t => t.UpdatedAt, now), cancellationToken);
            return;
        }

        var table = await _db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == tableId, cancellationToken);
        if (table is null)
        {
            return;
        }

        table.GuestSessionHash = null;
        table.GuestSessionExpiresAt = null;
        table.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsOccupiedByOtherAsync(int tableId, string? guestToken, CancellationToken cancellationToken = default)
    {
        var table = await _db.RestaurantTables
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tableId, cancellationToken);
        if (table is null)
        {
            return true;
        }

        if (string.IsNullOrEmpty(table.GuestSessionHash)
            || table.GuestSessionExpiresAt is not { } expires
            || expires < DateTime.UtcNow)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(guestToken))
        {
            return true;
        }

        return !string.Equals(table.GuestSessionHash, Hash(guestToken), StringComparison.Ordinal);
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
