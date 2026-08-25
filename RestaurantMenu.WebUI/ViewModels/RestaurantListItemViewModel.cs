namespace RestaurantMenu.WebUI.ViewModels;

public class RestaurantListItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int CategoryCount { get; set; }

    public int ProductCount { get; set; }

    public int TableCount { get; set; }

    public bool IsSelected { get; set; }
}
