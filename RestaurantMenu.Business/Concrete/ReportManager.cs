using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Dtos;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Enums;

namespace RestaurantMenu.Business.Concrete;

public class ReportManager : IReportService
{
    private readonly AppDbContext _db;

    public ReportManager(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStatsDto> GetDashboardAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayOrders = _db.Orders.Where(o =>
            o.Table.RestaurantId == restaurantId
            && o.CreatedAt >= today
            && o.CreatedAt < tomorrow
            && o.Status != OrderStatus.Cancelled);

        return new DashboardStatsDto
        {
            TodayOrderCount = await todayOrders.CountAsync(cancellationToken),
            TodaySales = await todayOrders.SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0,
            OpenOrderCount = await _db.Orders.CountAsync(
                o => o.Table.RestaurantId == restaurantId
                    && o.Status != OrderStatus.Completed
                    && o.Status != OrderStatus.Cancelled,
                cancellationToken),
            PendingServiceRequestCount = await _db.ServiceRequests.CountAsync(
                r => r.Table.RestaurantId == restaurantId && r.Status == ServiceRequestStatus.Pending,
                cancellationToken),
            ActiveTableCount = await _db.RestaurantTables.CountAsync(
                t => t.RestaurantId == restaurantId && t.IsActive,
                cancellationToken),
            AvailableProductCount = await _db.Products.CountAsync(
                p => p.Category.RestaurantId == restaurantId && p.IsActive && p.IsAvailable,
                cancellationToken)
        };
    }

    public async Task<SalesReportDto> GetSalesAsync(int restaurantId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            (from, to) = (to, from);
        }

        if ((to - from).TotalDays > 366)
        {
            to = from.AddDays(366);
        }

        var orders = _db.Orders.Where(o =>
            o.Table.RestaurantId == restaurantId
            && o.CreatedAt >= from
            && o.CreatedAt <= to
            && o.Status != OrderStatus.Cancelled);

        var top = await _db.OrderItems
            .Where(i => orders.Any(o => o.Id == i.OrderId))
            .GroupBy(i => i.ProductNameSnapshot)
            .Select(g => new ProductSalesRow
            {
                ProductName = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                Amount = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.Amount)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new SalesReportDto
        {
            From = from,
            To = to,
            OrderCount = await orders.CountAsync(cancellationToken),
            TotalSales = await orders.SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0,
            TopProducts = top
        };
    }
}
