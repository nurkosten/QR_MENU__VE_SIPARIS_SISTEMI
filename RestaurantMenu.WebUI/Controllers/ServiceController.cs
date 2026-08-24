using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Entities.Enums;

namespace RestaurantMenu.WebUI.Controllers;

public class ServiceController : Controller
{
    private readonly IMenuService _menuService;
    private readonly IServiceRequestService _serviceRequests;

    public ServiceController(IMenuService menuService, IServiceRequestService serviceRequests)
    {
        _menuService = menuService;
        _serviceRequests = serviceRequests;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestHelp(string restaurantToken, string tableToken, ServiceRequestType type)
    {
        var resolved = await _menuService.ResolveTableAsync(restaurantToken, tableToken);
        if (!resolved.Success)
        {
            return View("~/Views/Menu/InvalidQr.cshtml", resolved.Error);
        }

        var result = await _serviceRequests.CreateAsync(resolved.Data!.Table.Id, type);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? (type == ServiceRequestType.CallWaiter ? "Garson çağrınız iletildi." : "Hesap talebiniz iletildi.")
            : result.Error;

        return RedirectToAction("Index", "Menu", new { restaurantToken, tableToken });
    }
}
