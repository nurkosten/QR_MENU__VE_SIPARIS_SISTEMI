using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        var rows = new List<(ApplicationUser User, string Roles)>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add((user, string.Join(", ", roles)));
        }

        return View(rows);
    }

    public IActionResult Create()
    {
        return View(BuildForm(new UserFormViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildForm(model));
        }

        if (!AppRoles.All.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Geçersiz rol.");
            return View(BuildForm(model));
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FullName = model.FullName,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(BuildForm(model));
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        return RedirectToAction(nameof(Index));
    }

    private static UserFormViewModel BuildForm(UserFormViewModel model)
    {
        model.Roles = AppRoles.All.Select(r => new SelectListItem(r, r, r == model.Role));
        return model;
    }
}
