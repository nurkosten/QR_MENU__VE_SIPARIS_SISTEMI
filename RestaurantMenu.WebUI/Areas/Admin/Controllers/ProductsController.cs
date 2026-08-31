using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.Entities.Models;
using RestaurantMenu.WebUI.Infrastructure;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Managers)]
public class ProductsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ICurrentRestaurant _current;

    public ProductsController(AppDbContext db, IWebHostEnvironment env, ICurrentRestaurant current)
    {
        _db = db;
        _env = env;
        _current = current;
    }

    public async Task<IActionResult> Index()
    {
        var restaurantId = _current.Id!.Value;
        var list = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.Category.RestaurantId == restaurantId)
            .OrderBy(p => p.Name)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        return View(await BuildForm(new ProductFormViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model, IFormFile? image)
    {
        if (!await CategoryBelongsAsync(model.CategoryId))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Kategori bu restorana ait değil.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildForm(model));
        }

        try
        {
            var imageUrl = await ImageUpload.SaveAsync(image, "products", _env);
            _db.Products.Add(new Product
            {
                CategoryId = model.CategoryId,
                Name = model.Name.Trim(),
                Description = model.Description,
                Price = model.Price,
                IsAvailable = model.IsAvailable,
                IsActive = model.IsActive,
                ImageUrl = imageUrl
            });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(await BuildForm(model));
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var entity = await FindOwnedAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        return View(await BuildForm(new ProductFormViewModel
        {
            Id = entity.Id,
            CategoryId = entity.CategoryId,
            Name = entity.Name,
            Description = entity.Description,
            Price = entity.Price,
            IsAvailable = entity.IsAvailable,
            IsActive = entity.IsActive,
            ExistingImageUrl = entity.ImageUrl
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductFormViewModel model, IFormFile? image)
    {
        if (!await CategoryBelongsAsync(model.CategoryId))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Kategori bu restorana ait değil.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildForm(model));
        }

        var entity = await FindOwnedAsync(model.Id);
        if (entity is null)
        {
            return NotFound();
        }

        try
        {
            var imageUrl = await ImageUpload.SaveAsync(image, "products", _env);
            entity.CategoryId = model.CategoryId;
            entity.Name = model.Name.Trim();
            entity.Description = model.Description;
            entity.Price = model.Price;
            entity.IsAvailable = model.IsAvailable;
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            if (imageUrl is not null)
            {
                entity.ImageUrl = imageUrl;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.ExistingImageUrl = entity.ImageUrl;
            return View(await BuildForm(model));
        }
    }

    private Task<Product?> FindOwnedAsync(int id)
    {
        return _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.Category.RestaurantId == _current.Id);
    }

    private Task<bool> CategoryBelongsAsync(int categoryId)
    {
        return _db.Categories.AnyAsync(c => c.Id == categoryId && c.RestaurantId == _current.Id);
    }

    private async Task<ProductFormViewModel> BuildForm(ProductFormViewModel model)
    {
        var restaurantId = _current.Id!.Value;
        model.Categories = await _db.Categories
            .Where(c => c.RestaurantId == restaurantId)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == model.CategoryId))
            .ToListAsync();
        return model;
    }
}
