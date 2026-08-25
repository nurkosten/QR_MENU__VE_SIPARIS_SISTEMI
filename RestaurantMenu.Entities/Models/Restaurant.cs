using RestaurantMenu.Entities.Common;

namespace RestaurantMenu.Entities.Models;

public class Restaurant : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Description { get; set; }

    public string? WorkingHours { get; set; }

    public string PublicToken { get; set; } = string.Empty;

    public string MenuQrToken { get; set; } = string.Empty;

    public ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
