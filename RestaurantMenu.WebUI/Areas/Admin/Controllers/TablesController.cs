using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.Entities.Models;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class TablesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IQrCodeService _qr;

    public TablesController(AppDbContext db, IQrCodeService qr)
    {
        _db = db;
        _qr = qr;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.RestaurantTables.Include(t => t.Restaurant).OrderBy(t => t.TableNumber).ToListAsync();
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

        var restaurant = await _db.Restaurants.FirstAsync();
        var exists = await _db.RestaurantTables.AnyAsync(t => t.RestaurantId == restaurant.Id && t.TableNumber == model.TableNumber);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.TableNumber), "Bu masa numarası zaten kayıtlı.");
            return View(model);
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
        var entity = await _db.RestaurantTables.FindAsync(id);
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

        var entity = await _db.RestaurantTables.FindAsync(model.Id);
        if (entity is null)
        {
            return NotFound();
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
        var entity = await _db.RestaurantTables.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        entity.QrToken = _qr.CreateToken();
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "QR kodu yenilendi. Eski kod artık geçerli değil.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Qr(int id)
    {
        var table = await _db.RestaurantTables.Include(t => t.Restaurant).FirstOrDefaultAsync(t => t.Id == id);
        if (table is null)
        {
            return NotFound();
        }

        return View(table);
    }

    public async Task<IActionResult> QrImage(int id)
    {
        var table = await _db.RestaurantTables.Include(t => t.Restaurant).FirstOrDefaultAsync(t => t.Id == id);
        if (table is null)
        {
            return NotFound();
        }

        var url = Url.Action("Index", "Menu", new { area = "", restaurantToken = table.Restaurant.PublicToken, tableToken = table.QrToken }, Request.Scheme)!;
        var png = _qr.GeneratePng(url);
        return File(png, "image/png", $"masa-{table.TableNumber}-qr.png");
    }
}
