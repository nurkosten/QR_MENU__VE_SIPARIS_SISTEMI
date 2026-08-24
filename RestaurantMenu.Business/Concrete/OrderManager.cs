using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Common;
using RestaurantMenu.Business.Dtos;
using RestaurantMenu.Business.Orders;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.Business.Concrete;

public class OrderManager : IOrderService
{
    private readonly AppDbContext _db;
    private readonly IMenuService _menuService;

    public OrderManager(AppDbContext db, IMenuService menuService)
    {
        _db = db;
        _menuService = menuService;
    }

    public async Task<ServiceResult<Order>> CreateOrderAsync(
        int tableId,
        IReadOnlyList<CartLineInput> lines,
        string? customerNote,
        OrderStatus initialStatus = OrderStatus.New,
        CancellationToken cancellationToken = default)
    {
        if (lines is null || lines.Count == 0)
        {
            return ServiceResult<Order>.Fail("Sepet boş olamaz.");
        }

        if (initialStatus is not (OrderStatus.New or OrderStatus.Confirmed))
        {
            return ServiceResult<Order>.Fail("Sipariş yalnızca Yeni veya Onaylandı durumunda açılabilir.");
        }

        var table = await _db.RestaurantTables
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.Id == tableId, cancellationToken);

        if (table is null || !table.IsActive || !table.Restaurant.IsActive)
        {
            return ServiceResult<Order>.Fail("Masa bulunamadı veya aktif değil.");
        }

        var items = new List<OrderItem>();
        decimal total = 0;

        foreach (var line in lines)
        {
            if (line.Quantity <= 0 || line.Quantity > IOrderService.MaxQuantityPerLine)
            {
                return ServiceResult<Order>.Fail($"Adet 1 ile {IOrderService.MaxQuantityPerLine} arasında olmalıdır.");
            }

            var product = await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken);

            if (product is null ||
                !product.IsActive ||
                !product.IsAvailable ||
                !product.Category.IsActive ||
                product.Category.RestaurantId != table.RestaurantId)
            {
                return ServiceResult<Order>.Fail("Sepette satışı kapalı veya geçersiz bir ürün var. Lütfen menüyü yenileyin.");
            }

            items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                UnitPrice = product.Price,
                Quantity = line.Quantity,
                Note = Clip(line.Note, IOrderService.MaxLineNoteLength)
            });

            total += product.Price * line.Quantity;
        }

        var useTransaction = _db.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var order = new Order
            {
                TableId = table.Id,
                OrderNumber = CreateOrderNumber(),
                Status = initialStatus,
                TotalAmount = total,
                CustomerNote = Clip(customerNote, IOrderService.MaxNoteLength),
                Items = items
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return ServiceResult<Order>.Ok(order);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
    }

    public async Task<ServiceResult<Order>> PlaceCustomerOrderAsync(
        string restaurantToken,
        string tableToken,
        IReadOnlyList<CartLineInput> lines,
        string? customerNote,
        CancellationToken cancellationToken = default)
    {
        var tableResult = await _menuService.ResolveTableAsync(restaurantToken, tableToken, cancellationToken);
        if (!tableResult.Success)
        {
            return ServiceResult<Order>.Fail(tableResult.Error!);
        }

        return await CreateOrderAsync(
            tableResult.Data!.Table.Id,
            lines,
            customerNote,
            OrderStatus.Confirmed,
            cancellationToken);
    }

    public async Task<ServiceResult<Order>> ChangeStatusAsync(
        int orderId,
        OrderStatus nextStatus,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return ServiceResult<Order>.Fail("Sipariş bulunamadı.");
        }

        if (!OrderStatusMachine.CanTransition(order.Status, nextStatus))
        {
            return ServiceResult<Order>.Fail("Bu durum geçişine izin verilmiyor.");
        }

        order.Status = nextStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult<Order>.Ok(order);
    }

    public Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public Task<Order?> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetStaffOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items)
            .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.New
                || o.Status == OrderStatus.Confirmed
                || o.Status == OrderStatus.Preparing
                || o.Status == OrderStatus.Ready)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetAdminOrdersAsync(
        OrderStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status);
        }

        if (from.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= to.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    private static string CreateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    private static string? Clip(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
