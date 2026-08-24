using System.ComponentModel.DataAnnotations;

namespace RestaurantMenu.WebUI.ViewModels;

public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Kategori adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
