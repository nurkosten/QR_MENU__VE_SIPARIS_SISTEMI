using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.Infrastructure;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Managers)]
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
        var period = ResolveRange(range);
        ViewBag.Range = period.Key;
        return View(await _reports.GetSalesAsync(_current.Id!.Value, period.From, period.To));
    }

    public async Task<IActionResult> Pdf(string range = "daily")
    {
        var period = ResolveRange(range);
        var report = await _reports.GetSalesAsync(_current.Id!.Value, period.From, period.To);
        var restaurant = await _current.GetAsync();
        var bytes = SalesReportPdf.Create(restaurant?.Name ?? "Restoran", period.Label, report);
        var fileName = $"satis-raporu-{period.Slug}-{DateTime.Now:yyyyMMdd}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    private static (string Key, DateTime From, DateTime To, string Label, string Slug) ResolveRange(string range)
    {
        var now = DateTime.UtcNow;
        var key = range is "weekly" or "monthly" ? range : "daily";
        return key switch
        {
            "weekly" => (key, now.Date.AddDays(-7), now, "Son 7 gün", "haftalik"),
            "monthly" => (key, now.Date.AddDays(-30), now, "Son 30 gün", "aylik"),
            _ => (key, now.Date, now, "Bugün", "gunluk")
        };
    }
}
