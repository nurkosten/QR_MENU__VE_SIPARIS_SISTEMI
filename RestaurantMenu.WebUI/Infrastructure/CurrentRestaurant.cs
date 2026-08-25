using Microsoft.EntityFrameworkCore;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.WebUI.Infrastructure;

public class CurrentRestaurant : ICurrentRestaurant
{
    private readonly IHttpContextAccessor _http;
    private readonly AppDbContext _db;

    public CurrentRestaurant(IHttpContextAccessor http, AppDbContext db)
    {
        _http = http;
        _db = db;
    }

    public int? Id
    {
        get
        {
            var value = _http.HttpContext?.Session.GetInt32(ICurrentRestaurant.SessionKey);
            return value is > 0 ? value : null;
        }
    }

    public void Set(int id)
    {
        var session = _http.HttpContext?.Session
            ?? throw new InvalidOperationException("Oturum kullanılamıyor.");
        session.SetInt32(ICurrentRestaurant.SessionKey, id);
    }

    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        var selected = Id;
        if (selected.HasValue && await _db.Restaurants.AnyAsync(r => r.Id == selected.Value, cancellationToken))
        {
            return;
        }

        var firstId = await _db.Restaurants
            .AsNoTracking()
            .OrderByDescending(r => r.Tables.Any(t => t.IsActive))
            .ThenBy(r => r.Id)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (firstId > 0)
        {
            Set(firstId);
        }
    }

    public Task<Restaurant?> GetAsync(CancellationToken cancellationToken = default)
    {
        var id = Id;
        if (!id.HasValue)
        {
            return Task.FromResult<Restaurant?>(null);
        }

        return _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id.Value, cancellationToken);
    }

    public async Task<RestaurantTable?> GetPreviewTableAsync(CancellationToken cancellationToken = default)
    {
        var id = Id;
        if (!id.HasValue)
        {
            return null;
        }

        return await _db.RestaurantTables
            .AsNoTracking()
            .Include(t => t.Restaurant)
            .Where(t => t.RestaurantId == id.Value && t.IsActive && t.QrToken != "")
            .OrderBy(t => t.TableNumber)
            .ThenBy(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Restaurant>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Restaurants
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }
}
