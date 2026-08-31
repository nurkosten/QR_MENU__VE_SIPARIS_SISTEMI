using Microsoft.AspNetCore.Identity;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.Entities.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int? RestaurantId { get; set; }

    public Restaurant? Restaurant { get; set; }
}
