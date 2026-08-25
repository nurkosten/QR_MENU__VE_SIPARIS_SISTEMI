using RestaurantMenu.Business.Dtos;

namespace RestaurantMenu.Business.Abstract;

public interface IReportService
{
    Task<DashboardStatsDto> GetDashboardAsync(int restaurantId, CancellationToken cancellationToken = default);

    Task<SalesReportDto> GetSalesAsync(int restaurantId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
