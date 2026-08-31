using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Entities.Identity;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.DataAccess.Context;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(x => x.Restaurant)
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.ToTable("Restaurants");
            entity.Property(x => x.Name).IsRequired().HasMaxLength(150);
            entity.Property(x => x.LogoUrl).HasMaxLength(500);
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.Property(x => x.Phone).HasMaxLength(50);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.WorkingHours).HasMaxLength(200);
            entity.Property(x => x.PublicToken).IsRequired().HasMaxLength(64);
            entity.HasIndex(x => x.PublicToken).IsUnique();
            entity.Property(x => x.MenuQrToken).IsRequired().HasMaxLength(64);
            entity.HasIndex(x => x.MenuQrToken).IsUnique();
        });

        modelBuilder.Entity<RestaurantTable>(entity =>
        {
            entity.ToTable("Tables");
            entity.Property(x => x.Name).IsRequired().HasMaxLength(80);
            entity.Property(x => x.QrToken).IsRequired().HasMaxLength(64);
            entity.HasIndex(x => x.QrToken).IsUnique();
            entity.HasIndex(x => new { x.RestaurantId, x.TableNumber }).IsUnique();
            entity.HasOne(x => x.Restaurant)
                .WithMany(x => x.Tables)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => new { x.RestaurantId, x.DisplayOrder });
            entity.HasOne(x => x.Restaurant)
                .WithMany(x => x.Categories)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.Property(x => x.Name).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => new { x.CategoryId, x.IsActive, x.IsAvailable });
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.Property(x => x.OrderNumber).IsRequired().HasMaxLength(40);
            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.CustomerNote).HasMaxLength(500);
            entity.HasOne(x => x.Table)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.Property(x => x.ProductNameSnapshot).IsRequired().HasMaxLength(150);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Note).HasMaxLength(250);
            entity.HasOne(x => x.Order)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.ToTable("ServiceRequests");
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
            entity.HasOne(x => x.Table)
                .WithMany(x => x.ServiceRequests)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.HandledBy)
                .WithMany()
                .HasForeignKey(x => x.HandledByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("ActivityLogs");
            entity.Property(x => x.Level).IsRequired().HasMaxLength(20);
            entity.Property(x => x.Category).IsRequired().HasMaxLength(40);
            entity.Property(x => x.Message).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.UserName).HasMaxLength(256);
            entity.Property(x => x.Path).HasMaxLength(400);
            entity.Property(x => x.HttpMethod).HasMaxLength(16);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => new { x.Level, x.CreatedAt });
        });
    }
}
