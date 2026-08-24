using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.Business.Dtos;

public class MenuContextDto
{
    public Restaurant Restaurant { get; set; } = null!;

    public RestaurantTable Table { get; set; } = null!;

    public IReadOnlyList<Category> Categories { get; set; } = [];
}
