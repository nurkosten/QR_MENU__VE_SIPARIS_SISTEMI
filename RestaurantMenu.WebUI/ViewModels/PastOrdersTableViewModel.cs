using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.WebUI.ViewModels;

public class PastOrdersTableViewModel
{
    public IReadOnlyList<Order> Orders { get; set; } = [];

    public bool AllowComplete { get; set; }
}
