using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Orders;
using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.Infrastructure;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class OrdersController : Controller
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    public async Task<IActionResult> Index(OrderStatus? status)
    {
        ViewBag.Status = status;
        return View(await _orders.GetAdminOrdersAsync(status, null, null));
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        return order is null ? NotFound() : View(order);
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

        if (!OrderStatusMachine.CanTransition(order.Status, nextStatus))
        {
            TempData["Error"] = "Bu durum geçişine izin verilmiyor.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _orders.ChangeStatusAsync(id, nextStatus);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Sipariş durumu güncellendi." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }
}
