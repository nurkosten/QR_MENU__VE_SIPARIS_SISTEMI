using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.Entities.Models;
using RestaurantMenu.WebUI.Infrastructure;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Managers)]
public class TablesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IQrCodeService _qr;
    private readonly ICurrentRestaurant _current;

    public TablesController(AppDbContext db, IQrCodeService qr, ICurrentRestaurant current)
    {
        _db = db;
        _qr = qr;
        _current = current;
    }

    public async Task<IActionResult> Index()
    {
        var restaurantId = _current.Id!.Value;
        var list = await _db.RestaurantTables
            .Include(t => t.Restaurant)
            .Where(t => t.RestaurantId == restaurantId)
            .OrderBy(t => t.TableNumber)
            .ToListAsync();
        return View(list);
    }

    public IActionResult Create() => View(new TableFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TableFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var restaurant = await _db.Restaurants.FirstOrDefaultAsync(r => r.Id == _current.Id);
        if (restaurant is null)
        {
            return NotFound();
        }

        var exists = await _db.RestaurantTables.AnyAsync(t => t.RestaurantId == restaurant.Id && t.TableNumber == model.TableNumber);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.TableNumber), "Bu masa numarası zaten kayıtlı.");
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(restaurant.MenuQrToken))
        {
            restaurant.MenuQrToken = _qr.CreateToken();
            restaurant.UpdatedAt = DateTime.UtcNow;
        }

        _db.RestaurantTables.Add(new RestaurantTable
        {
            RestaurantId = restaurant.Id,
            TableNumber = model.TableNumber,
            Name = model.Name.Trim(),
            QrToken = _qr.CreateToken(),
            IsActive = model.IsActive
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var entity = await FindOwnedAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        return View(new TableFormViewModel
        {
            Id = entity.Id,
            TableNumber = entity.TableNumber,
            Name = entity.Name,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TableFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var entity = await FindOwnedAsync(model.Id);
        if (entity is null)
        {
            return NotFound();
        }

        var duplicate = await _db.RestaurantTables.AnyAsync(t =>
            t.RestaurantId == entity.RestaurantId && t.TableNumber == model.TableNumber && t.Id != entity.Id);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(model.TableNumber), "Bu masa numarası zaten kayıtlı.");
            return View(model);
        }

        entity.TableNumber = model.TableNumber;
        entity.Name = model.Name.Trim();
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenewQr(int id)
    {
        var table = await FindOwnedAsync(id);
        if (table is null)
        {
            return NotFound();
        }

        table.QrToken = _qr.CreateToken();
        table.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"{table.Name} QR kodu yenilendi. Bu masanın eski basılı kodu artık geçerli değil.";
        return RedirectToAction(nameof(Qr), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenewAllQr()
    {
        var tables = await _db.RestaurantTables
            .Where(t => t.RestaurantId == _current.Id)
            .ToListAsync();
        if (tables.Count == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var table in tables)
        {
            table.QrToken = _qr.CreateToken();
            table.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Her masanın QR kodu ayrı ayrı yenilendi. Eski basılı kodlar artık geçerli değil.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Qr(int id)
    {
        var table = await _db.RestaurantTables
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.Id == id && t.RestaurantId == _current.Id);
        if (table is null)
        {
            return NotFound();
        }

        return View(table);
    }

    public async Task<IActionResult> QrImage(int id)
    {
        var table = await _db.RestaurantTables
            .Include(t => t.Restaurant)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.RestaurantId == _current.Id);
        if (table is null || string.IsNullOrWhiteSpace(table.QrToken))
        {
            return NotFound();
        }

        var url = Url.Action("Index", "Menu", new { area = "", restaurantToken = table.Restaurant.PublicToken, tableToken = table.QrToken }, Request.Scheme)!;
        var png = _qr.GeneratePng(url);
        return File(png, "image/png", $"{table.Name}-qr.png");
    }

    private Task<RestaurantTable?> FindOwnedAsync(int id)
    {
        return _db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == id && t.RestaurantId == _current.Id);
    }
}
