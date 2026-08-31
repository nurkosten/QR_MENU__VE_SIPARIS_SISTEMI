using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.Entities.Models;
using RestaurantMenu.WebUI.Infrastructure;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Managers)]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentRestaurant _current;

    public CategoriesController(AppDbContext db, ICurrentRestaurant current)
    {
        _db = db;
        _current = current;
    }

    public async Task<IActionResult> Index()
    {
        var restaurantId = _current.Id!.Value;
        var list = await _db.Categories
            .Where(c => c.RestaurantId == restaurantId)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
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

        _db.Categories.Add(new Category
        {
            RestaurantId = _current.Id!.Value,
            Name = model.Name.Trim(),
            DisplayOrder = model.DisplayOrder,
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

        var entity = await FindOwnedAsync(model.Id);
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

    private Task<Category?> FindOwnedAsync(int id)
    {
        return _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.RestaurantId == _current.Id);
    }
}
