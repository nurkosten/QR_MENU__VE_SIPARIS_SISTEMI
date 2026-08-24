using Microsoft.AspNetCore.Identity;

namespace RestaurantMenu.Entities.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
