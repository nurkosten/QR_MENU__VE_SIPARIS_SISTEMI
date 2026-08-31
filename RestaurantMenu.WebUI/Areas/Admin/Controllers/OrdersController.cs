using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Orders;
using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.Entities.Models;
using RestaurantMenu.WebUI.Infrastructure;

namespace RestaurantMenu.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Managers)]
public class OrdersController : Controller
{
    private readonly IOrderService _orders;
    private readonly ICurrentRestaurant _current;

    public OrdersController(IOrderService orders, ICurrentRestaurant current)
    {
        _orders = orders;
        _current = current;
    }

    public async Task<IActionResult> Index(OrderStatus? status)
    {
        ViewBag.Status = status;
        return View(await _orders.GetAdminOrdersAsync(_current.Id!.Value, status, null, null));
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await FindOwnedAsync(id);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus nextStatus)
    {
        var order = await FindOwnedAsync(id);
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

    private async Task<Order?> FindOwnedAsync(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        return order is not null && order.Table.RestaurantId == _current.Id ? order : null;
    }
}
