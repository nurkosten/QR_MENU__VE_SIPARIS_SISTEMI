namespace RestaurantMenu.Entities.Identity;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Personel = "Personel";
    public const string Mutfak = "Mutfak";

    public static readonly string[] All = [Admin, Personel, Mutfak];
}
