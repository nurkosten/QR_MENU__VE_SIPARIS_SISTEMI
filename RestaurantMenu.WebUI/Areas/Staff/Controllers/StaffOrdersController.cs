using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Orders;
using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.WebUI.Infrastructure;

namespace RestaurantMenu.WebUI.Areas.Staff.Controllers;

[Area("Staff")]
[Authorize(Roles = $"{AppRoles.Personel},{AppRoles.Admin}")]
public class StaffOrdersController : Controller
{
    private readonly IOrderService _orders;
    private readonly IServiceRequestService _requests;
    private readonly UserManager<ApplicationUser> _users;

    public StaffOrdersController(
        IOrderService orders,
        IServiceRequestService requests,
        UserManager<ApplicationUser> users)
    {
        _orders = orders;
        _requests = requests;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var openRequests = await _requests.GetOpenAsync();
        ViewBag.Requests = openRequests;
        return View(await _orders.GetStaffOrdersAsync());
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HandleRequest(int id, ServiceRequestStatus status)
    {
        var user = await _users.GetUserAsync(User);
        var result = await _requests.ChangeStatusAsync(id, status, user!.Id);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Talep güncellendi." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
