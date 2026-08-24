using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RestaurantMenu.WebUI.ViewModels;

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Kategori")]
    public int CategoryId { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Ürün adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required]
    [Range(0.01, 999999)]
    [Display(Name = "Fiyat")]
    public decimal Price { get; set; }

    [Display(Name = "Satışta")]
    public bool IsAvailable { get; set; } = true;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public string? ExistingImageUrl { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = [];
}
