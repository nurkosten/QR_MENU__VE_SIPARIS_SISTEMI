using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.DataAccess.Context;

namespace RestaurantMenu.WebUI.Infrastructure;

public static class RestaurantSlug
{
    public static async Task<string> UniquePublicTokenAsync(
        AppDbContext db,
        string name,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var slug = FromName(name);
        var candidate = slug;
        var suffix = 2;
        while (await db.Restaurants.AnyAsync(
                   r => r.PublicToken == candidate && (!excludeId.HasValue || r.Id != excludeId.Value),
                   cancellationToken))
        {
            candidate = $"{slug}-{suffix++}";
        }

        return candidate;
    }

    public static string FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "restoran";
        }

        var normalized = name.Trim()
            .Replace("ı", "i", StringComparison.Ordinal)
            .Replace("İ", "i", StringComparison.Ordinal)
            .Replace("ş", "s", StringComparison.Ordinal)
            .Replace("Ş", "s", StringComparison.Ordinal)
            .Replace("ğ", "g", StringComparison.Ordinal)
            .Replace("Ğ", "g", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal)
            .Replace("Ü", "u", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace("Ö", "o", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal)
            .Replace("Ç", "c", StringComparison.Ordinal)
            .ToLower(CultureInfo.InvariantCulture);

        var builder = new StringBuilder(normalized.Length);
        var pendingHyphen = false;
        foreach (var ch in normalized)
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                if (pendingHyphen && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(ch);
                pendingHyphen = false;
            }
            else
            {
                pendingHyphen = builder.Length > 0;
            }
        }

        var slug = builder.ToString();
        if (slug.Length > 40)
        {
            slug = slug[..40].TrimEnd('-');
        }

        return string.IsNullOrEmpty(slug) ? "restoran" : slug;
    }
}
