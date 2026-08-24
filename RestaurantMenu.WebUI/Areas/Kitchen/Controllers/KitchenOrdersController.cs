using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Orders;
using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.Infrastructure;

namespace RestaurantMenu.WebUI.Areas.Kitchen.Controllers;

[Area("Kitchen")]
[Authorize(Roles = $"{AppRoles.Mutfak},{AppRoles.Admin}")]
public class KitchenOrdersController : Controller
{
    private readonly IOrderService _orders;

    public KitchenOrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _orders.GetKitchenOrdersAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus nextStatus)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        var roles = OrderStatusPolicy.ResolveRoles(User);
        if (!OrderStatusPolicy.CanChange(roles, order.Status, nextStatus) || !OrderStatusMachine.CanTransition(order.Status, nextStatus))
        {
            TempData["Error"] = "Bu durum geçişine izin verilmiyor.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _orders.ChangeStatusAsync(id, nextStatus);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Sipariş güncellendi." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
