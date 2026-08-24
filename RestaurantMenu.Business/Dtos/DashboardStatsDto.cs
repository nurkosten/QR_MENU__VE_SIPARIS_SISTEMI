namespace RestaurantMenu.Business.Dtos;

public class DashboardStatsDto
{
    public int TodayOrderCount { get; set; }

    public decimal TodaySales { get; set; }

    public int OpenOrderCount { get; set; }

    public int PendingServiceRequestCount { get; set; }

    public int ActiveTableCount { get; set; }

    public int AvailableProductCount { get; set; }
}
