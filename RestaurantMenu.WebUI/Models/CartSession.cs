using RestaurantMenu.Business.Dtos;

namespace RestaurantMenu.WebUI.Models;

public class CartSession
{
    public int TableId { get; set; }

    public string RestaurantToken { get; set; } = string.Empty;

    public string TableToken { get; set; } = string.Empty;

    public string TableName { get; set; } = string.Empty;

    public List<CartLineInput> Lines { get; set; } = [];
}
