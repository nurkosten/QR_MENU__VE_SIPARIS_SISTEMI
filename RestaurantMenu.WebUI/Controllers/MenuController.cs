using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.Infrastructure;
using RestaurantMenu.WebUI.Models;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.Controllers;

[AllowAnonymous]
public class MenuController : Controller
{
    private readonly IMenuService _menuService;
    private readonly ICurrentRestaurant _current;

    public MenuController(IMenuService menuService, ICurrentRestaurant current)
    {
        _menuService = menuService;
        _current = current;
    }

    [HttpGet("/menu/{restaurantToken}/{tableToken}")]
    public async Task<IActionResult> Index(
        string restaurantToken,
        string tableToken,
        string? q,
        int? categoryId,
        int? tableId,
        int? preview)
    {
        if (preview == 1 && IsStaffPreview())
        {
            var selected = await _current.GetPreviewTableAsync();
            if (selected is { Restaurant.IsActive: true }
                && (!string.Equals(selected.Restaurant.PublicToken, restaurantToken, StringComparison.Ordinal)
                    || !string.Equals(selected.QrToken, tableToken, StringComparison.Ordinal)))
            {
                return RedirectToAction(nameof(Index), new
                {
                    restaurantToken = selected.Restaurant.PublicToken,
                    tableToken = selected.QrToken,
                    q,
                    categoryId,
                    preview = 1
                });
            }

            if (selected is null && _current.Id is > 0)
            {
                return RedirectToAction(nameof(Index), "Home");
            }
        }

        ViewBag.PreviewMode = preview == 1 && IsStaffPreview();

        var menu = await _menuService.GetMenuAsync(restaurantToken, tableToken, tableId);
        if (!menu.Success)
        {
            return View("InvalidQr", menu.Error);
        }

        var cart = HttpContext.Session.GetCart() ?? new CartSession();
        if (!string.Equals(cart.RestaurantToken, restaurantToken, StringComparison.Ordinal))
        {
            cart.Lines.Clear();
        }

        cart.RestaurantToken = restaurantToken;
        cart.TableToken = tableToken;
        if (menu.Data!.Table is not null)
        {
            cart.TableId = menu.Data.Table.Id;
            cart.TableName = menu.Data.Table.Name;
        }

        HttpContext.Session.SetCart(cart);

        var model = new MenuPageViewModel
        {
            Restaurant = menu.Data.Restaurant,
            Table = menu.Data.Table,
            ActiveTables = menu.Data.ActiveTables,
            MenuQrToken = tableToken,
            Categories = menu.Data.Categories,
            Search = q,
            CategoryId = categoryId,
            CartCount = cart.Lines.Sum(x => x.Quantity)
        };

        if (model.Table is not null && (!string.IsNullOrWhiteSpace(q) || categoryId is > 0))
        {
            var products = await _menuService.SearchProductsAsync(menu.Data.Restaurant.Id, q, categoryId);
            foreach (var category in menu.Data.Categories)
            {
                category.Products = products.Where(p => p.CategoryId == category.Id).ToList();
            }

            model.Categories = menu.Data.Categories
                .Where(c => c.Products.Count > 0 || categoryId == c.Id)
                .ToList();
        }

        return View(model);
    }

    private bool IsStaffPreview() =>
        User.Identity?.IsAuthenticated == true
        && (User.IsInRole(AppRoles.Admin)
            || User.IsInRole(AppRoles.Sahip)
            || User.IsInRole(AppRoles.Personel)
            || User.IsInRole(AppRoles.Mutfak));
}
