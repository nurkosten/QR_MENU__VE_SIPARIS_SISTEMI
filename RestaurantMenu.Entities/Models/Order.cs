using RestaurantMenu.Entities.Common;
using RestaurantMenu.Entities.Enums;

namespace RestaurantMenu.Entities.Models;

public class Order : BaseEntity
{
    public int TableId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public decimal TotalAmount { get; set; }

    public string? CustomerNote { get; set; }

    public RestaurantTable Table { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
