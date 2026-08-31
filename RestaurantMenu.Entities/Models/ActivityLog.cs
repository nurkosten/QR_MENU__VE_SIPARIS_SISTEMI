namespace RestaurantMenu.Entities.Models;

public class ActivityLog
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Level { get; set; } = "Info";

    public string Category { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string? Path { get; set; }

    public string? HttpMethod { get; set; }

    public int? StatusCode { get; set; }

    public int? RestaurantId { get; set; }
}
