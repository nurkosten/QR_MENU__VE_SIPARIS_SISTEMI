using System.ComponentModel.DataAnnotations;

namespace RestaurantMenu.WebUI.ViewModels;

public class RestaurantFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "İşletme adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Adres")]
    public string? Address { get; set; }

    [StringLength(50)]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [StringLength(1000)]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [StringLength(200)]
    [Display(Name = "Çalışma saatleri")]
    public string? WorkingHours { get; set; }

    [StringLength(64)]
    [Display(Name = "Genel erişim kodu")]
    public string? PublicToken { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public string? ExistingLogoUrl { get; set; }
}
