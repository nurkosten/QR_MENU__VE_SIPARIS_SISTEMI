using RestaurantMenu.Business.Common;
using RestaurantMenu.Business.Dtos;
using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.Business.Abstract;

public interface IOrderService
{
    const int MaxQuantityPerLine = 20;

    const int MaxNoteLength = 500;
    const int MaxLineNoteLength = 250;

    Task<ServiceResult<Order>> CreateOrderAsync(
        int tableId,
        IReadOnlyList<CartLineInput> lines,
        string? customerNote,
        OrderStatus initialStatus = OrderStatus.New,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<Order>> PlaceCustomerOrderAsync(
        string restaurantToken,
        string tableToken,
        IReadOnlyList<CartLineInput> lines,
        string? customerNote,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<Order>> ChangeStatusAsync(
        int orderId,
        OrderStatus nextStatus,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);

    Task<Order?> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetStaffOrdersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetAdminOrdersAsync(OrderStatus? status, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
