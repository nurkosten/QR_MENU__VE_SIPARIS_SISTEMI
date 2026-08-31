using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Managers)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public UsersController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var scope = await OwnerScopeAsync();
        var query = _userManager.Users.AsNoTracking().Include(u => u.Restaurant).AsQueryable();
        if (scope is not null)
        {
            query = query.Where(u => u.RestaurantId == scope);
        }

        var users = await query.OrderBy(u => u.Email).ToListAsync();
        var rows = new List<(ApplicationUser User, string Roles)>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (scope is not null && roles.Any(r => r is AppRoles.Admin or AppRoles.Sahip))
            {
                continue;
            }

            rows.Add((user, string.Join(", ", roles.Select(AppRoles.DisplayName))));
        }

        return View(rows);
    }

    public async Task<IActionResult> Create()
    {
        return View(await BuildFormAsync(new UserFormViewModel { RequirePassword = true }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        model.RequirePassword = true;
        await ApplyOwnerRulesAsync(model);
        ValidateAssignment(model);
        if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 8)
        {
            ModelState.AddModelError(nameof(model.Password), "Şifre en az 8 karakter olmalıdır.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FullName = model.FullName,
            IsActive = true,
            RestaurantId = AssignedRestaurantId(model)
        };

        var result = await _userManager.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(await BuildFormAsync(model));
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null || !await CanManageAsync(user))
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return View(await BuildFormAsync(new UserFormViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? string.Empty,
            RestaurantId = user.RestaurantId,
            RequirePassword = false
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserFormViewModel model)
    {
        model.Id = id;
        model.RequirePassword = false;
        var user = await _userManager.FindByIdAsync(id);
        if (user is null || !await CanManageAsync(user))
        {
            return NotFound();
        }

        await ApplyOwnerRulesAsync(model);
        ValidateAssignment(model);
        if (!string.IsNullOrWhiteSpace(model.Password) && model.Password.Length < 8)
        {
            ModelState.AddModelError(nameof(model.Password), "Şifre en az 8 karakter olmalıdır.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        user.Email = model.Email;
        user.UserName = model.Email;
        user.FullName = model.FullName;
        user.RestaurantId = AssignedRestaurantId(model);

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var error in update.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(await BuildFormAsync(model));
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(nameof(model.Password), error.Description);
                }

                return View(await BuildFormAsync(model));
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> OwnerScopeAsync()
    {
        if (User.IsInRole(AppRoles.Admin))
        {
            return null;
        }

        var current = await _userManager.GetUserAsync(User);
        return current?.RestaurantId ?? 0;
    }

    private async Task ApplyOwnerRulesAsync(UserFormViewModel model)
    {
        var scope = await OwnerScopeAsync();
        if (scope is null)
        {
            return;
        }

        if (scope is not > 0)
        {
            ModelState.AddModelError(string.Empty, "Hesabınız bir restorana bağlı değil.");
            return;
        }

        model.RestaurantId = scope;
        if (!AppRoles.OwnerAssignable.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Restoran sahibi yalnızca personel ve mutfak hesabı ekleyebilir.");
        }
    }

    private async Task<bool> CanManageAsync(ApplicationUser user)
    {
        var scope = await OwnerScopeAsync();
        if (scope is null)
        {
            return true;
        }

        if (scope is not > 0 || user.RestaurantId != scope)
        {
            return false;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.All(r => AppRoles.OwnerAssignable.Contains(r));
    }

    private void ValidateAssignment(UserFormViewModel model)
    {
        var allowed = User.IsInRole(AppRoles.Admin) ? AppRoles.All : AppRoles.OwnerAssignable;
        if (!allowed.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Geçersiz rol.");
        }

        if (AppRoles.RequiresRestaurant(model.Role) && model.RestaurantId is not > 0)
        {
            ModelState.AddModelError(nameof(model.RestaurantId), "Bu rol bir restorana bağlanmalıdır.");
        }
    }

    private static int? AssignedRestaurantId(UserFormViewModel model) =>
        AppRoles.RequiresRestaurant(model.Role) ? model.RestaurantId : null;

    private async Task<UserFormViewModel> BuildFormAsync(UserFormViewModel model)
    {
        var scope = await OwnerScopeAsync();
        var roleOptions = User.IsInRole(AppRoles.Admin) ? AppRoles.All : AppRoles.OwnerAssignable;
        model.CanPickRole = true;
        model.Roles = roleOptions.Select(r => new SelectListItem(AppRoles.DisplayName(r), r, r == model.Role));
        model.CanPickRestaurant = scope is null;
        if (scope is > 0)
        {
            model.RestaurantId = scope;
        }

        var restaurantsQuery = _db.Restaurants.AsNoTracking();
        if (scope is not null)
        {
            restaurantsQuery = restaurantsQuery.Where(r => r.Id == scope);
        }

        var restaurants = await restaurantsQuery
            .OrderBy(r => r.Name)
            .Select(r => new SelectListItem(r.Name, r.Id.ToString(), r.Id == model.RestaurantId))
            .ToListAsync();
        if (scope is null)
        {
            restaurants.Insert(0, new SelectListItem("Restoran seçin", "", model.RestaurantId is not > 0));
        }

        model.Restaurants = restaurants;
        return model;
    }
}
