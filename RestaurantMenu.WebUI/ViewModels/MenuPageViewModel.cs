using RestaurantMenu.Business.Dtos;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.WebUI.ViewModels;

public class MenuPageViewModel
{
    public Restaurant Restaurant { get; set; } = null!;

    public RestaurantTable Table { get; set; } = null!;

    public IReadOnlyList<Category> Categories { get; set; } = [];

    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    public int CartCount { get; set; }
}

public class CartPageViewModel
{
    public RestaurantTable Table { get; set; } = null!;

    public Restaurant Restaurant { get; set; } = null!;

    public List<CartLineView> Lines { get; set; } = [];

    public decimal Total => Lines.Sum(x => x.LineTotal);

    public string? CustomerNote { get; set; }
}

public class CartLineView
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public string? Note { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
