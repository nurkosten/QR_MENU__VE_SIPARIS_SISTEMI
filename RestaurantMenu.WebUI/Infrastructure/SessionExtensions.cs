using System.Text.Json;
using RestaurantMenu.WebUI.Models;

namespace RestaurantMenu.WebUI.Infrastructure;

public static class SessionExtensions
{
    public const string CartKey = "cart";

    public static void SetCart(this ISession session, CartSession cart)
    {
        session.SetString(CartKey, JsonSerializer.Serialize(cart));
    }

    public static CartSession? GetCart(this ISession session)
    {
        var json = session.GetString(CartKey);
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<CartSession>(json);
    }

    public static void ClearCart(this ISession session) => session.Remove(CartKey);
}
