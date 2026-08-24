namespace RestaurantMenu.Business.Dtos;

public class CartLineInput
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public string? Note { get; set; }
}
