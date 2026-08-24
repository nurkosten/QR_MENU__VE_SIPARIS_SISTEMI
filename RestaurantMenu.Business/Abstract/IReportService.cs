using RestaurantMenu.Business.Dtos;

namespace RestaurantMenu.Business.Abstract;

public interface IReportService
{
    Task<DashboardStatsDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<SalesReportDto> GetSalesAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
