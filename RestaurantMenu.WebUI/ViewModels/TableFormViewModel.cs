using System.ComponentModel.DataAnnotations;

namespace RestaurantMenu.WebUI.ViewModels;

public class TableFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Range(1, 999)]
    [Display(Name = "Masa no")]
    public int TableNumber { get; set; }

    [Required, StringLength(80)]
    [Display(Name = "Masa adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
