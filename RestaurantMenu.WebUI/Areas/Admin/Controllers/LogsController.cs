using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Entities.Identity;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class LogsController : Controller
{
    private readonly IActivityLogService _logs;

    public LogsController(IActivityLogService logs)
    {
        _logs = logs;
    }

    public async Task<IActionResult> Index(string? level)
    {
        var normalized = level is "Info" or "Warning" or "Error" ? level : null;
        ViewBag.Level = normalized;
        return View(await _logs.ListRecentAsync(400, normalized));
    }
}
