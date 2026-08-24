namespace RestaurantMenu.Entities.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int? ProductId { get; set; }

    public string ProductNameSnapshot { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public string? Note { get; set; }

    public Order Order { get; set; } = null!;

    public Product? Product { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
