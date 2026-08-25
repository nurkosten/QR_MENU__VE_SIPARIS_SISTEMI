using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.WebUI.ViewModels;

public class RestaurantSwitcherViewModel
{
    public IReadOnlyList<Restaurant> Restaurants { get; set; } = [];

    public int? SelectedId { get; set; }
}
