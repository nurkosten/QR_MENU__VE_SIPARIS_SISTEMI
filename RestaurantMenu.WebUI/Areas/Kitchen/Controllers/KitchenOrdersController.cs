using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Dtos;
using RestaurantMenu.Business.Orders;
using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.Infrastructure;

namespace RestaurantMenu.WebUI.Areas.Kitchen.Controllers;

[Area("Kitchen")]
[Authorize(Roles = $"{AppRoles.Mutfak},{AppRoles.Admin},{AppRoles.Sahip}")]
public class KitchenOrdersController : Controller
{
    private readonly IOrderService _orders;
    private readonly ICurrentRestaurant _current;

    public KitchenOrdersController(IOrderService orders, ICurrentRestaurant current)
    {
        _orders = orders;
        _current = current;
    }

    public async Task<IActionResult> Index()
    {
        var restaurantId = _current.Id!.Value;
        ViewBag.OtherWork = User.IsInRole(AppRoles.Admin)
            ? await _orders.GetKitchenWorkElsewhereAsync(restaurantId)
            : Array.Empty<RestaurantWorkDto>();
        ViewBag.PastOrders = await _orders.GetPastOrdersAsync(restaurantId);
        return View(await _orders.GetKitchenOrdersAsync(restaurantId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus nextStatus)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order is null || order.Table.RestaurantId != _current.Id)
        {
            return NotFound();
        }

        if (!OrderStatusPolicy.CanKitchenChange(order.Status, nextStatus)
            || !OrderStatusMachine.CanTransition(order.Status, nextStatus))
        {
            TempData["Error"] = "Mutfak yalnızca hazırlığı güncelleyebilir. Servis ve tamamlandı garson panelinden yapılır.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _orders.ChangeStatusAsync(id, nextStatus);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Sipariş güncellendi." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
