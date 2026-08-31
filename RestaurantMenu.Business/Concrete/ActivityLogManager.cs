using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Dtos;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.Business.Concrete;

public class ActivityLogManager : IActivityLogService
{
    private readonly AppDbContext _db;

    public ActivityLogManager(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ActivityLogEntry entry, CancellationToken cancellationToken = default)
    {
        _db.ActivityLogs.Add(new ActivityLog
        {
            CreatedAt = DateTime.UtcNow,
            Level = Clip(entry.Level, 20) ?? "Info",
            Category = Clip(entry.Category, 40) ?? "Genel",
            Message = Clip(entry.Message, 1000) ?? string.Empty,
            UserName = Clip(entry.UserName, 256),
            Path = Clip(entry.Path, 400),
            HttpMethod = Clip(entry.HttpMethod, 16),
            StatusCode = entry.StatusCode,
            RestaurantId = entry.RestaurantId
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActivityLog>> ListRecentAsync(int take = 400, string? level = null, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 1000);
        var query = _db.ActivityLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(level))
        {
            query = query.Where(x => x.Level == level);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteOlderThanDaysAsync(int days, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 3650);
        var cutoff = DateTime.UtcNow.AddDays(-days);
        await _db.ActivityLogs.Where(x => x.CreatedAt < cutoff).ExecuteDeleteAsync(cancellationToken);
    }

    private static string? Clip(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
