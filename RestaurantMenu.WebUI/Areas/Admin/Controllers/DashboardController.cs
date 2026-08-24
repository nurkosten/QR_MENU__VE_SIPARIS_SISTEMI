using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Entities.Identity;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class DashboardController : Controller
{
    private readonly IReportService _reports;

    public DashboardController(IReportService reports)
    {
        _reports = reports;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _reports.GetDashboardAsync());
    }
}
