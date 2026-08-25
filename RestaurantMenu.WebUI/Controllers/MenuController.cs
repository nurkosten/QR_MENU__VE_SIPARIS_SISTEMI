using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.WebUI.Infrastructure;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.Controllers;

[AllowAnonymous]
public class MenuController : Controller
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet("/menu/{restaurantToken}/{tableToken}")]
    public async Task<IActionResult> Index(string restaurantToken, string tableToken, string? q, int? categoryId, int? tableId)
    {
        var menu = await _menuService.GetMenuAsync(restaurantToken, tableToken, tableId);
        if (!menu.Success)
        {
            return View("InvalidQr", menu.Error);
        }

        var cart = HttpContext.Session.GetCart();
        var model = new MenuPageViewModel
        {
            Restaurant = menu.Data!.Restaurant,
            Table = menu.Data.Table,
            ActiveTables = menu.Data.ActiveTables,
            MenuQrToken = tableToken,
            Categories = menu.Data.Categories,
            Search = q,
            CategoryId = categoryId,
            CartCount = cart?.Lines.Sum(x => x.Quantity) ?? 0
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
}
