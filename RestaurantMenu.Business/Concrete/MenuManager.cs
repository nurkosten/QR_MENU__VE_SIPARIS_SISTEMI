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
        int? tableId = null,
        CancellationToken cancellationToken = default)
    {
        return await ResolveTableQrAsync(restaurantToken, tableToken, tableId, cancellationToken);
    }

    public async Task<ServiceResult<MenuContextDto>> GetMenuAsync(
        string restaurantToken,
        string tableToken,
        int? tableId = null,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveTableQrAsync(restaurantToken, tableToken, tableId, cancellationToken);
        if (!resolved.Success)
        {
            return ServiceResult<MenuContextDto>.Fail(resolved.Error!);
        }

        var restaurant = resolved.Data!.Restaurant;
        var table = resolved.Data.Table;
        var categories = await _db.Categories
            .Where(c => c.RestaurantId == restaurant.Id && c.IsActive)
            .Include(c => c.Products.Where(p => p.IsActive && p.IsAvailable))
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return ServiceResult<MenuContextDto>.Ok(new MenuContextDto
        {
            Restaurant = restaurant,
            Table = table,
            ActiveTables = [table],
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

    private async Task<ServiceResult<(Restaurant Restaurant, RestaurantTable Table)>> ResolveTableQrAsync(
        string restaurantToken,
        string tableToken,
        int? tableId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(restaurantToken) || string.IsNullOrWhiteSpace(tableToken))
        {
            return ServiceResult<(Restaurant, RestaurantTable)>.Fail("Geçersiz QR kodu.");
        }

        var table = await _db.RestaurantTables
            .AsNoTracking()
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(
                t => t.QrToken == tableToken && t.Restaurant.PublicToken == restaurantToken,
                cancellationToken);

        if (table is null)
        {
            return ServiceResult<(Restaurant, RestaurantTable)>.Fail("QR kodu bulunamadı.");
        }

        if (!table.Restaurant.IsActive)
        {
            return ServiceResult<(Restaurant, RestaurantTable)>.Fail("QR kodu bu işletme ile eşleşmiyor.");
        }

        if (!table.IsActive)
        {
            return ServiceResult<(Restaurant, RestaurantTable)>.Fail("Bu masa şu anda aktif değil.");
        }

        if (tableId is > 0 && table.Id != tableId)
        {
            return ServiceResult<(Restaurant, RestaurantTable)>.Fail("QR kodu bu masaya ait değil.");
        }

        return ServiceResult<(Restaurant, RestaurantTable)>.Ok((table.Restaurant, table));
    }
}
