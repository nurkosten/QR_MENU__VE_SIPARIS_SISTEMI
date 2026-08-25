using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.Infrastructure;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class DashboardController : Controller
{
    private readonly IReportService _reports;
    private readonly ICurrentRestaurant _current;

    public DashboardController(IReportService reports, ICurrentRestaurant current)
    {
        _reports = reports;
        _current = current;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _reports.GetDashboardAsync(_current.Id!.Value));
    }
}
