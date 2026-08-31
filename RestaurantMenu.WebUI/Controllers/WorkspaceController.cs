using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.Infrastructure;

namespace RestaurantMenu.WebUI.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Sahip},{AppRoles.Personel},{AppRoles.Mutfak}")]
public class WorkspaceController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentRestaurant _current;

    public WorkspaceController(AppDbContext db, ICurrentRestaurant current)
    {
        _db = db;
        _current = current;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(int id)
    {
        if (!User.IsInRole(AppRoles.Admin))
        {
            return Forbid();
        }

        var exists = await _db.Restaurants.AnyAsync(r => r.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        _current.Set(id);
        HttpContext.Session.ClearCart();
        TempData["CustomerMenuPreviewReload"] = "1";

        var referer = Request.GetTypedHeaders().Referer;
        if (referer is not null)
        {
            var local = referer.IsAbsoluteUri ? referer.PathAndQuery : referer.ToString();
            if (Url.IsLocalUrl(local))
            {
                return Redirect(local);
            }
        }

        if (User.IsInRole(AppRoles.Admin))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        if (User.IsInRole(AppRoles.Personel))
        {
            return RedirectToAction("Index", "StaffOrders", new { area = "Staff" });
        }

        return RedirectToAction("Index", "KitchenOrders", new { area = "Kitchen" });
    }
}
