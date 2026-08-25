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
    private readonly IReportService _reports;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ICurrentRestaurant _current;

    public StaffOrdersController(
        IOrderService orders,
        IServiceRequestService requests,
        IReportService reports,
        UserManager<ApplicationUser> users,
        ICurrentRestaurant current)
    {
        _orders = orders;
        _requests = requests;
        _reports = reports;
        _users = users;
        _current = current;
    }

    public async Task<IActionResult> Index()
    {
        var restaurantId = _current.Id!.Value;
        var openRequests = await _requests.GetOpenAsync(restaurantId);
        ViewBag.Requests = openRequests;
        var stats = await _reports.GetDashboardAsync(restaurantId);
        ViewBag.TodayOrderCount = stats.TodayOrderCount;
        ViewBag.OtherWork = await _orders.GetStaffWorkElsewhereAsync(restaurantId);
        ViewBag.PastOrders = await _orders.GetPastOrdersAsync(restaurantId);
        return View(await _orders.GetStaffOrdersAsync(restaurantId));
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

        if (!OrderStatusPolicy.CanStaffChange(order.Status, nextStatus)
            || !OrderStatusMachine.CanTransition(order.Status, nextStatus))
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
        var result = await _requests.ChangeStatusAsync(id, status, user!.Id, _current.Id!.Value);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Talep güncellendi." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
