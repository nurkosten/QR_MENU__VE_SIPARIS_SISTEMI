using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.Entities.Models;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
        return View(list);
    }

    public IActionResult Create() => View(new CategoryFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var restaurantId = await GetRestaurantIdAsync();
        _db.Categories.Add(new Category
        {
            RestaurantId = restaurantId,
            Name = model.Name.Trim(),
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _db.Categories.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        return View(new CategoryFormViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            DisplayOrder = entity.DisplayOrder,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var entity = await _db.Categories.FindAsync(model.Id);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Name = model.Name.Trim();
        entity.DisplayOrder = model.DisplayOrder;
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<int> GetRestaurantIdAsync()
    {
        var id = await _db.Restaurants.Select(r => r.Id).FirstAsync();
        return id;
    }
}
