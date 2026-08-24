using RestaurantMenu.Entities.Common;

namespace RestaurantMenu.Entities.Models;

public class Category : BaseEntity
{
    public int RestaurantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public Restaurant Restaurant { get; set; } = null!;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
