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
[Authorize(Roles = AppRoles.Admin)]
public class RestaurantController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IQrCodeService _qr;
    private readonly ICurrentRestaurant _current;

    public RestaurantController(AppDbContext db, IWebHostEnvironment env, IQrCodeService qr, ICurrentRestaurant current)
    {
        _db = db;
        _env = env;
        _qr = qr;
        _current = current;
    }

    public async Task<IActionResult> Index()
    {
        await _current.EnsureAsync();
        var selectedId = _current.Id;
        var list = await _db.Restaurants
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RestaurantListItemViewModel
            {
                Id = r.Id,
                Name = r.Name,
                IsActive = r.IsActive,
                CategoryCount = r.Categories.Count,
                ProductCount = r.Categories.Sum(c => c.Products.Count),
                TableCount = r.Tables.Count,
                IsSelected = selectedId.HasValue && r.Id == selectedId.Value
            })
            .ToListAsync();

        return View(list);
    }

    public IActionResult Create() => View(new RestaurantFormViewModel { IsActive = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RestaurantFormViewModel model, IFormFile? logo)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var publicToken = string.IsNullOrWhiteSpace(model.PublicToken)
            ? await RestaurantSlug.UniquePublicTokenAsync(_db, model.Name)
            : model.PublicToken.Trim();

        if (await _db.Restaurants.AnyAsync(r => r.PublicToken == publicToken))
        {
            ModelState.AddModelError(nameof(model.PublicToken), "Bu genel erişim kodu başka bir restoranda kullanılıyor.");
            return View(model);
        }

        try
        {
            var entity = new Restaurant
            {
                Name = model.Name.Trim(),
                Address = model.Address,
                Phone = model.Phone,
                Description = model.Description,
                WorkingHours = model.WorkingHours,
                PublicToken = publicToken,
                MenuQrToken = _qr.CreateToken(),
                IsActive = model.IsActive,
                LogoUrl = await ImageUpload.SaveAsync(logo, "logos", _env)
            };

            _db.Restaurants.Add(entity);
            await _db.SaveChangesAsync();
            _current.Set(entity.Id);
            TempData["Success"] = "Restoran eklendi. Menü ve masaları bu restoran için tanımlayabilirsiniz.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int? id)
    {
        await _current.EnsureAsync();
        var restaurantId = id ?? _current.Id;
        if (restaurantId is null)
        {
            return RedirectToAction(nameof(Create));
        }

        var entity = await _db.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId);
        if (entity is null)
        {
            return NotFound();
        }

        return View(ToForm(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RestaurantFormViewModel model, IFormFile? logo)
    {
        if (string.IsNullOrWhiteSpace(model.PublicToken))
        {
            ModelState.AddModelError(nameof(model.PublicToken), "Genel erişim kodu gerekli.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var entity = await _db.Restaurants.FirstOrDefaultAsync(r => r.Id == model.Id);
        if (entity is null)
        {
            return NotFound();
        }

        var publicToken = model.PublicToken!.Trim();
        if (await _db.Restaurants.AnyAsync(r => r.PublicToken == publicToken && r.Id != entity.Id))
        {
            ModelState.AddModelError(nameof(model.PublicToken), "Bu genel erişim kodu başka bir restoranda kullanılıyor.");
            model.ExistingLogoUrl = entity.LogoUrl;
            return View(model);
        }

        try
        {
            var logoUrl = await ImageUpload.SaveAsync(logo, "logos", _env);
            entity.Name = model.Name.Trim();
            entity.Address = model.Address;
            entity.Phone = model.Phone;
            entity.Description = model.Description;
            entity.WorkingHours = model.WorkingHours;
            entity.PublicToken = publicToken;
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            if (logoUrl is not null)
            {
                entity.LogoUrl = logoUrl;
            }

            await _db.SaveChangesAsync();
            _current.Set(entity.Id);
            TempData["Success"] = "İşletme bilgileri güncellendi.";
            return RedirectToAction(nameof(Edit), new { id = entity.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.ExistingLogoUrl = entity.LogoUrl;
            return View(model);
        }
    }

    private static RestaurantFormViewModel ToForm(Restaurant entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Address = entity.Address,
        Phone = entity.Phone,
        Description = entity.Description,
        WorkingHours = entity.WorkingHours,
        PublicToken = entity.PublicToken,
        IsActive = entity.IsActive,
        ExistingLogoUrl = entity.LogoUrl
    };
}
