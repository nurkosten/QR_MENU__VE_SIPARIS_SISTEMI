using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.DataAccess.Context;

namespace RestaurantMenu.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var table = await _db.RestaurantTables
            .AsNoTracking()
            .Include(t => t.Restaurant)
            .Where(t => t.IsActive && t.Restaurant.IsActive)
            .OrderBy(t => t.TableNumber)
            .FirstOrDefaultAsync();

        if (table is not null)
        {
            return RedirectToAction("Index", "Menu", new
            {
                restaurantToken = table.Restaurant.PublicToken,
                tableToken = table.QrToken
            });
        }

        var restaurant = await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync();
        return View(restaurant);
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
}
