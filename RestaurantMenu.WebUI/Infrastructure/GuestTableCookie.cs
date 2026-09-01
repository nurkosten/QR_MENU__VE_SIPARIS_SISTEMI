using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.WebUI.Infrastructure;

public static class GuestTableCookie
{
    public const string Name = "rm.guest";

    public const string OccupiedMessage =
        "Bu masa başka bir tarayıcı oturumunda açık. Gizli sekme veya kopyalanan bağlantı kabul edilmez. Masadaki QR kodunu ilk okuttuğunuz tarayıcıyı kullanın ya da personelden masayı serbest bırakmasını isteyin.";

    public const string MissingSessionMessage =
        "Menü oturumu bulunamadı. Masadaki QR kodunu gizli sekme dışında, normal tarayıcıda okutun.";

    public static bool IsStaff(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
        && (user.IsInRole(AppRoles.Admin)
            || user.IsInRole(AppRoles.Sahip)
            || user.IsInRole(AppRoles.Personel)
            || user.IsInRole(AppRoles.Mutfak));

    public static string? Read(HttpContext http) =>
        http.Request.Cookies.TryGetValue(Name, out var token) && IsToken(token) ? token : null;

    public static void PreventCaching(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        http.Response.Headers.Pragma = "no-cache";
    }

    public static async Task<IActionResult?> ClaimAsync(
        Controller controller,
        ITableGuestSession sessions,
        RestaurantTable table)
    {
        PreventCaching(controller.HttpContext);
        if (IsStaff(controller.User))
        {
            return null;
        }

        var token = Read(controller.HttpContext) ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        if (!await sessions.TryBindAsync(table.Id, token))
        {
            return controller.View("~/Views/Menu/InvalidQr.cshtml", OccupiedMessage);
        }

        Write(controller.HttpContext, token);
        return null;
    }

    public static async Task<IActionResult?> RequireBoundAsync(
        Controller controller,
        ITableGuestSession sessions,
        RestaurantTable table)
    {
        PreventCaching(controller.HttpContext);
        if (IsStaff(controller.User))
        {
            return null;
        }

        var token = Read(controller.HttpContext);
        if (token is null)
        {
            return controller.View("~/Views/Menu/InvalidQr.cshtml", MissingSessionMessage);
        }

        if (!await sessions.TryBindAsync(table.Id, token))
        {
            return controller.View("~/Views/Menu/InvalidQr.cshtml", OccupiedMessage);
        }

        Write(controller.HttpContext, token);
        return null;
    }

    private static void Write(HttpContext http, string token)
    {
        http.Response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps,
            MaxAge = TimeSpan.FromHours(4),
            Path = "/"
        });
    }

    private static bool IsToken(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);
}
