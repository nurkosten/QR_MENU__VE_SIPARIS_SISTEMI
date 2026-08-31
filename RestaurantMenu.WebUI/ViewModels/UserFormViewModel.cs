using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RestaurantMenu.WebUI.ViewModels;

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required, EmailAddress]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Ad soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Rol")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Restoran")]
    public int? RestaurantId { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string? Password { get; set; }

    public bool RequirePassword { get; set; } = true;

    public bool CanPickRestaurant { get; set; } = true;

    public bool CanPickRole { get; set; } = true;

    public IEnumerable<SelectListItem> Roles { get; set; } = [];

    public IEnumerable<SelectListItem> Restaurants { get; set; } = [];
}
