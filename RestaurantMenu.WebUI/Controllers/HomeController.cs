using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.WebUI.Infrastructure;

namespace RestaurantMenu.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentRestaurant _current;

    public HomeController(AppDbContext db, ICurrentRestaurant current)
    {
        _db = db;
        _current = current;
    }

    public async Task<IActionResult> Index()
    {
        var selectedTable = await _current.GetPreviewTableAsync();
        if (selectedTable is { Restaurant.IsActive: true })
        {
            return RedirectToMenu(
                selectedTable.Restaurant.PublicToken,
                selectedTable.QrToken,
                preview: true);
        }

        if (_current.Id is > 0)
        {
            ViewBag.PreviewMode = true;
            return View(await _current.GetAsync());
        }

        var table = await _db.RestaurantTables
            .AsNoTracking()
            .Include(t => t.Restaurant)
            .Where(t => t.IsActive && t.Restaurant.IsActive && t.QrToken != "")
            .OrderBy(t => t.RestaurantId)
            .ThenBy(t => t.TableNumber)
            .ThenBy(t => t.Id)
            .FirstOrDefaultAsync();

        if (table is not null)
        {
            return RedirectToMenu(table.Restaurant.PublicToken, table.QrToken);
        }

        return View(await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.IsActive));
    }

    public IActionResult Error()
    {
        return View();
    }

    public IActionResult NotFoundPage()
    {
        Response.StatusCode = 404;
        return View("NotFound");
    }

    private RedirectToActionResult RedirectToMenu(string restaurantToken, string tableToken, bool preview = false)
    {
        var cart = HttpContext.Session.GetCart();
        if (cart is not null && !string.Equals(cart.RestaurantToken, restaurantToken, StringComparison.Ordinal))
        {
            HttpContext.Session.ClearCart();
        }

        if (preview)
        {
            return RedirectToAction("Index", "Menu", new { restaurantToken, tableToken, preview = 1 });
        }

        return RedirectToAction("Index", "Menu", new { restaurantToken, tableToken });
    }
}
