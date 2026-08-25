using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Dtos;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.WebUI.Infrastructure;
using RestaurantMenu.WebUI.Models;
using RestaurantMenu.WebUI.ViewModels;

namespace RestaurantMenu.WebUI.Controllers;

[AllowAnonymous]
public class CartController : Controller
{
    private readonly IMenuService _menuService;
    private readonly IOrderService _orderService;
    private readonly AppDbContext _db;

    public CartController(IMenuService menuService, IOrderService orderService, AppDbContext db)
    {
        _menuService = menuService;
        _orderService = orderService;
        _db = db;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string restaurantToken, string tableToken, int tableId, int productId, int quantity = 1, string? note = null)
    {
        var resolved = await _menuService.ResolveTableAsync(restaurantToken, tableToken, tableId);
        if (!resolved.Success)
        {
            return View("~/Views/Menu/InvalidQr.cshtml", resolved.Error);
        }

        if (quantity <= 0 || quantity > IOrderService.MaxQuantityPerLine)
        {
            TempData["Error"] = $"Adet 1 ile {IOrderService.MaxQuantityPerLine} arasında olmalıdır.";
            return RedirectToAction("Index", "Menu", new { restaurantToken, tableToken, tableId });
        }

        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null || !product.IsActive || !product.IsAvailable || !product.Category.IsActive)
        {
            TempData["Error"] = "Bu ürün şu anda satışta değil.";
            return RedirectToAction("Index", "Menu", new { restaurantToken, tableToken, tableId });
        }

        var table = resolved.Data!.Table;
        if (product.Category.RestaurantId != table.RestaurantId)
        {
            TempData["Error"] = "Bu ürün bu masanın menüsünde yok.";
            return RedirectToAction("Index", "Menu", new { restaurantToken, tableToken, tableId });
        }
        var cart = HttpContext.Session.GetCart() ?? new CartSession();
        if (cart.TableId != 0 && cart.TableId != table.Id)
        {
            cart.Lines.Clear();
        }

        cart.TableId = table.Id;
        cart.RestaurantToken = restaurantToken;
        cart.TableToken = tableToken;
        cart.TableName = table.Name;

        var existing = cart.Lines.FirstOrDefault(x => x.ProductId == productId && x.Note == note);
        if (existing is null)
        {
            cart.Lines.Add(new CartLineInput { ProductId = productId, Quantity = quantity, Note = note });
        }
        else
        {
            existing.Quantity = Math.Min(IOrderService.MaxQuantityPerLine, existing.Quantity + quantity);
        }

        HttpContext.Session.SetCart(cart);
        TempData["Success"] = $"{product.Name} sepete eklendi.";
        return RedirectToAction("Index", "Menu", new { restaurantToken, tableToken, tableId });
    }

    public async Task<IActionResult> Index()
    {
        var cart = HttpContext.Session.GetCart();
        if (cart is null || cart.Lines.Count == 0)
        {
            return View(new CartPageViewModel());
        }

        var resolved = await _menuService.ResolveTableAsync(cart.RestaurantToken, cart.TableToken, cart.TableId);
        if (!resolved.Success)
        {
            HttpContext.Session.ClearCart();
            return View("~/Views/Menu/InvalidQr.cshtml", resolved.Error);
        }

        return View(await BuildPage(cart, resolved.Data!.Restaurant, resolved.Data.Table));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Update(int productId, int quantity, string? note)
    {
        var cart = HttpContext.Session.GetCart();
        if (cart is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var line = cart.Lines.FirstOrDefault(x => x.ProductId == productId && x.Note == note);
        if (line is not null)
        {
            if (quantity <= 0)
            {
                cart.Lines.Remove(line);
            }
            else
            {
                line.Quantity = Math.Min(IOrderService.MaxQuantityPerLine, quantity);
            }
        }

        HttpContext.Session.SetCart(cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int productId, string? note)
    {
        var cart = HttpContext.Session.GetCart();
        cart?.Lines.RemoveAll(x => x.ProductId == productId && x.Note == note);
        if (cart is not null)
        {
            HttpContext.Session.SetCart(cart);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string? customerNote)
    {
        var cart = HttpContext.Session.GetCart();
        if (cart is null || cart.Lines.Count == 0)
        {
            TempData["Error"] = "Sepet boş.";
            return RedirectToAction(nameof(Index));
        }

        var resolved = await _menuService.ResolveTableAsync(cart.RestaurantToken, cart.TableToken, cart.TableId);
        if (!resolved.Success)
        {
            return View("~/Views/Menu/InvalidQr.cshtml", resolved.Error);
        }

        var result = await _orderService.PlaceCustomerOrderAsync(
            cart.RestaurantToken,
            cart.TableToken,
            cart.Lines,
            customerNote,
            cart.TableId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.ClearCart();
        return RedirectToAction("Status", "CustomerOrders", new
        {
            number = result.Data!.OrderNumber,
            restaurantToken = cart.RestaurantToken,
            tableToken = cart.TableToken,
            tableId = result.Data.TableId
        });
    }

    private async Task<CartPageViewModel> BuildPage(CartSession cart, Entities.Models.Restaurant restaurant, Entities.Models.RestaurantTable table)
    {
        var ids = cart.Lines.Select(x => x.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        return new CartPageViewModel
        {
            Restaurant = restaurant,
            Table = table,
            Lines = cart.Lines.Select(line =>
            {
                var product = products.FirstOrDefault(p => p.Id == line.ProductId);
                return new CartLineView
                {
                    ProductId = line.ProductId,
                    Name = product?.Name ?? "Ürün",
                    UnitPrice = product?.Price ?? 0,
                    Quantity = line.Quantity,
                    Note = line.Note
                };
            }).ToList()
        };
    }
}
