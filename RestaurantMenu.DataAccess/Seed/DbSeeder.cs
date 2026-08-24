using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.DataAccess.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        await db.Database.MigrateAsync();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(userManager, "admin@restaurant.local", "Admin123!", "Sistem Yöneticisi", AppRoles.Admin);
        await EnsureUserAsync(userManager, "personel@restaurant.local", "Personel123!", "Garson", AppRoles.Personel);
        await EnsureUserAsync(userManager, "mutfak@restaurant.local", "Mutfak123!", "Mutfak", AppRoles.Mutfak);

        if (await db.Restaurants.AnyAsync())
        {
            return;
        }

        var restaurant = new Restaurant
        {
            Name = "Nur Burger",
            Address = "Malatya / Yeşilyurt",
            Phone = "0422 000 00 00",
            Description = "QR kod ile masadan sipariş verebileceğiniz demo restoran.",
            WorkingHours = "11:00 - 23:00",
            PublicToken = "nur-burger",
            IsActive = true
        };

        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        var burgers = new Category { RestaurantId = restaurant.Id, Name = "Burgerler", DisplayOrder = 1, IsActive = true };
        var drinks = new Category { RestaurantId = restaurant.Id, Name = "İçecekler", DisplayOrder = 2, IsActive = true };
        db.Categories.AddRange(burgers, drinks);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product
            {
                CategoryId = burgers.Id,
                Name = "Cheeseburger",
                Description = "Dana eti, cheddar peyniri, marul ve özel sos.",
                Price = 220m,
                IsAvailable = true,
                IsActive = true
            },
            new Product
            {
                CategoryId = drinks.Id,
                Name = "Kola",
                Description = "330 ml kutu kola.",
                Price = 45m,
                IsAvailable = true,
                IsActive = true
            });

        db.RestaurantTables.Add(new RestaurantTable
        {
            RestaurantId = restaurant.Id,
            TableNumber = 8,
            Name = "Masa 8",
            QrToken = Guid.NewGuid().ToString("N"),
            IsActive = true
        });

        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Seed kullanıcı oluşturulamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, role);
    }
}
