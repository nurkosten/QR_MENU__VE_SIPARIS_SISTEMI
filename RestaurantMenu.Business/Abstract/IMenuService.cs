using RestaurantMenu.Business.Common;
using RestaurantMenu.Business.Dtos;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.Business.Abstract;

public interface IMenuService
{
    Task<ServiceResult<MenuContextDto>> GetMenuAsync(
        string restaurantToken,
        string tableToken,
        int? tableId = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<(Restaurant Restaurant, RestaurantTable Table)>> ResolveTableAsync(
        string restaurantToken,
        string tableToken,
        int? tableId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> SearchProductsAsync(
        int restaurantId,
        string? term,
        int? categoryId,
        CancellationToken cancellationToken = default);
}
