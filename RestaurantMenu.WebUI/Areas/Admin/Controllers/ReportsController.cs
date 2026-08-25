using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.Infrastructure;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class ReportsController : Controller
{
    private readonly IReportService _reports;
    private readonly ICurrentRestaurant _current;

    public ReportsController(IReportService reports, ICurrentRestaurant current)
    {
        _reports = reports;
        _current = current;
    }

    public async Task<IActionResult> Index(string range = "daily")
    {
        var now = DateTime.UtcNow;
        var (from, to) = range switch
        {
            "weekly" => (now.Date.AddDays(-7), now),
            "monthly" => (now.Date.AddDays(-30), now),
            _ => (now.Date, now)
        };

        ViewBag.Range = range;
        return View(await _reports.GetSalesAsync(_current.Id!.Value, from, to));
    }
}
