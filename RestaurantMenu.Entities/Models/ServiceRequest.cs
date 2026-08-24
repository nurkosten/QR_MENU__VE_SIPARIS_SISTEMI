using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Identity;

namespace RestaurantMenu.Entities.Models;

public class ServiceRequest
{
    public int Id { get; set; }

    public int TableId { get; set; }

    public ServiceRequestType Type { get; set; }

    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? HandledAt { get; set; }

    public string? HandledByUserId { get; set; }

    public RestaurantTable Table { get; set; } = null!;

    public ApplicationUser? HandledBy { get; set; }
}
