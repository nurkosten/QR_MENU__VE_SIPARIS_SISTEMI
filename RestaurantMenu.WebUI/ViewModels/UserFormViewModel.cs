using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RestaurantMenu.WebUI.ViewModels;

public class UserFormViewModel
{
    [Required, EmailAddress]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Ad soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Rol")]
    public string Role { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(8)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> Roles { get; set; } = [];
}
