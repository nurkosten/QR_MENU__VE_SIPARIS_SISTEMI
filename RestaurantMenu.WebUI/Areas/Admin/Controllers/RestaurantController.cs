using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.Infrastructure;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class RestaurantController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public RestaurantController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Edit()
    {
        var entity = await _db.Restaurants.FirstAsync();
        return View(new RestaurantFormViewModel
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
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RestaurantFormViewModel model, IFormFile? logo)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var entity = await _db.Restaurants.FirstAsync(r => r.Id == model.Id);
        try
        {
            var logoUrl = await ImageUpload.SaveAsync(logo, "logos", _env);
            entity.Name = model.Name.Trim();
            entity.Address = model.Address;
            entity.Phone = model.Phone;
            entity.Description = model.Description;
            entity.WorkingHours = model.WorkingHours;
            entity.PublicToken = model.PublicToken.Trim();
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            if (logoUrl is not null)
            {
                entity.LogoUrl = logoUrl;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "İşletme bilgileri güncellendi.";
            return RedirectToAction(nameof(Edit));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.ExistingLogoUrl = entity.LogoUrl;
            return View(model);
        }
    }
}
