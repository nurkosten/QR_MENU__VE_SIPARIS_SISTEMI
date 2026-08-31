namespace RestaurantMenu.Entities.Identity;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Sahip = "Sahip";
    public const string Personel = "Personel";
    public const string Mutfak = "Mutfak";

    public const string Managers = Admin + "," + Sahip;

    public static readonly string[] All = [Admin, Sahip, Personel, Mutfak];

    public static readonly string[] OwnerAssignable = [Personel, Mutfak];

    public static bool RequiresRestaurant(string? role) =>
        role is Sahip or Personel or Mutfak;

    public static string DisplayName(string role) => role switch
    {
        Admin => "Yönetici",
        Sahip => "Restoran sahibi",
        Personel => "Personel",
        Mutfak => "Mutfak",
        _ => role
    };
}
