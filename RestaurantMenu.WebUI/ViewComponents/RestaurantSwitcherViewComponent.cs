using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.WebUI.Infrastructure;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.ViewComponents;

public class RestaurantSwitcherViewComponent : ViewComponent
{
    private readonly ICurrentRestaurant _current;

    public RestaurantSwitcherViewComponent(ICurrentRestaurant current)
    {
        _current = current;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        await _current.EnsureAsync();
        return View(new RestaurantSwitcherViewModel
        {
            Restaurants = await _current.ListAsync(),
            SelectedId = _current.Id,
            CanSwitch = _current.CanSwitch
        });
    }
}
