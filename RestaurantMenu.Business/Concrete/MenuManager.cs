using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Common;
using RestaurantMenu.Business.Dtos;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.Business.Concrete;

public class MenuManager : IMenuService
{
    private readonly AppDbContext _db;

    public MenuManager(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<(Restaurant Restaurant, RestaurantTable Table)>> ResolveTableAsync(
        string restaurantToken,
        string tableToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(restaurantToken) || string.IsNullOrWhiteSpace(tableToken))
        {
            return ServiceResult<(Restaurant, RestaurantTable)>.Fail("Geçersiz QR kodu.");
        }

        var table = await _db.RestaurantTables
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.QrToken == tableToken, cancellationToken);

        if (table is null)
        {
            return ServiceResult<(Restaurant, RestaurantTable)>.Fail("QR kodu bulunamadı.");
        }

        if (!table.IsActive)
        {
            return ServiceResult<(Restaurant, RestaurantTable)>.Fail("Bu masa şu anda aktif değil.");
        }

        if (!table.Restaurant.IsActive ||
            !string.Equals(table.Restaurant.PublicToken, restaurantToken, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<(Restaurant, RestaurantTable)>.Fail("QR kodu bu işletme ile eşleşmiyor.");
        }

        return ServiceResult<(Restaurant, RestaurantTable)>.Ok((table.Restaurant, table));
    }

    public async Task<ServiceResult<MenuContextDto>> GetMenuAsync(
        string restaurantToken,
        string tableToken,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveTableAsync(restaurantToken, tableToken, cancellationToken);
        if (!resolved.Success)
        {
            return ServiceResult<MenuContextDto>.Fail(resolved.Error!);
        }

        var restaurantId = resolved.Data!.Restaurant.Id;

        var categories = await _db.Categories
            .Where(c => c.RestaurantId == restaurantId && c.IsActive)
            .Include(c => c.Products.Where(p => p.IsActive && p.IsAvailable))
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return ServiceResult<MenuContextDto>.Ok(new MenuContextDto
        {
            Restaurant = resolved.Data.Restaurant,
            Table = resolved.Data.Table,
            Categories = categories
        });
    }

    public async Task<IReadOnlyList<Product>> SearchProductsAsync(
        int restaurantId,
        string? term,
        int? categoryId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Where(p => p.Category.RestaurantId == restaurantId && p.IsActive && p.IsAvailable && p.Category.IsActive);

        if (categoryId is > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var like = term.Trim();
            query = query.Where(p => p.Name.Contains(like) || (p.Description != null && p.Description.Contains(like)));
        }

        return await query
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
