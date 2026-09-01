using RestaurantMenu.Entities.Common;

namespace RestaurantMenu.Entities.Models;

public class RestaurantTable : BaseEntity
{
    public int RestaurantId { get; set; }

    public int TableNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public string QrToken { get; set; } = string.Empty;

    public string? GuestSessionHash { get; set; }

    public DateTime? GuestSessionExpiresAt { get; set; }

    public Restaurant Restaurant { get; set; } = null!;

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
