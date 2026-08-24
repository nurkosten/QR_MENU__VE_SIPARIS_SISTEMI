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

    public async Task<IActionResult> Status(string number, string restaurantToken, string tableToken)
    {
        var resolved = await _menuService.ResolveTableAsync(restaurantToken, tableToken);
        if (!resolved.Success)
        {
            return View("~/Views/Menu/InvalidQr.cshtml", resolved.Error);
        }

        var order = await _orderService.GetByNumberAsync(number);
        if (order is null || order.TableId != resolved.Data!.Table.Id)
        {
            return NotFound();
        }

        ViewBag.RestaurantToken = restaurantToken;
        ViewBag.TableToken = tableToken;
        return View(order);
    }

    public async Task<IActionResult> StatusJson(string number, string restaurantToken, string tableToken)
    {
        var resolved = await _menuService.ResolveTableAsync(restaurantToken, tableToken);
        if (!resolved.Success)
        {
            return NotFound();
        }

        var order = await _orderService.GetByNumberAsync(number);
        if (order is null || order.TableId != resolved.Data!.Table.Id)
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
