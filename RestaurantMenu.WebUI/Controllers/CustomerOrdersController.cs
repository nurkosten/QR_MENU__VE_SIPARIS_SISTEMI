using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.WebUI.Infrastructure;

namespace RestaurantMenu.WebUI.Controllers;

public class CustomerOrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IMenuService _menuService;

    public CustomerOrdersController(IOrderService orderService, IMenuService menuService)
    {
        _orderService = orderService;
        _menuService = menuService;
    }

    public async Task<IActionResult> Status(string number, string restaurantToken, string tableToken, int? tableId)
    {
        var order = await _orderService.GetByNumberAsync(number);
        if (order is null)
        {
            return NotFound();
        }

        var resolved = await _menuService.ResolveTableAsync(restaurantToken, tableToken, tableId ?? order.TableId);
        if (!resolved.Success || order.TableId != resolved.Data!.Table.Id)
        {
            return View("~/Views/Menu/InvalidQr.cshtml", resolved.Success ? "Sipariş bu masa ile eşleşmiyor." : resolved.Error);
        }

        ViewBag.RestaurantToken = restaurantToken;
        ViewBag.TableToken = tableToken;
        ViewBag.TableId = order.TableId;
        return View(order);
    }

    public async Task<IActionResult> StatusJson(string number, string restaurantToken, string tableToken, int? tableId)
    {
        var order = await _orderService.GetByNumberAsync(number);
        if (order is null)
        {
            return NotFound();
        }

        var resolved = await _menuService.ResolveTableAsync(restaurantToken, tableToken, tableId ?? order.TableId);
        if (!resolved.Success || order.TableId != resolved.Data!.Table.Id)
        {
            return NotFound();
        }

        return Json(new
        {
            order.OrderNumber,
            status = order.Status.ToString(),
            statusText = DisplayTexts.OrderStatus(order.Status)
        });
    }
}
