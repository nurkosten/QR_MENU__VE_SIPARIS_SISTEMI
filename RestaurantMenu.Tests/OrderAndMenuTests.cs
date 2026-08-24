using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Concrete;
using RestaurantMenu.Business.Dtos;
using RestaurantMenu.Business.Orders;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.Tests;

public class OrderStatusMachineTests
{
    [Fact]
    public void New_order_can_be_confirmed_or_cancelled()
    {
        Assert.True(OrderStatusMachine.CanTransition(OrderStatus.New, OrderStatus.Confirmed));
        Assert.True(OrderStatusMachine.CanTransition(OrderStatus.New, OrderStatus.Cancelled));
        Assert.False(OrderStatusMachine.CanTransition(OrderStatus.New, OrderStatus.Ready));
    }

    [Fact]
    public void Cancelled_is_terminal()
    {
        Assert.Empty(OrderStatusMachine.GetAllowedTargets(OrderStatus.Cancelled));
        Assert.False(OrderStatusMachine.CanTransition(OrderStatus.Cancelled, OrderStatus.New));
    }
}

public class OrderManagerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var restaurant = new Restaurant { Name = "Test", PublicToken = "test-rest", IsActive = true };
        db.Restaurants.Add(restaurant);
        db.SaveChanges();
        var category = new Category { RestaurantId = restaurant.Id, Name = "Burgerler", DisplayOrder = 1, IsActive = true };
        db.Categories.Add(category);
        db.SaveChanges();
        db.Products.Add(new Product
        {
            CategoryId = category.Id,
            Name = "Cheeseburger",
            Price = 220m,
            IsActive = true,
            IsAvailable = true
        });
        db.RestaurantTables.Add(new RestaurantTable
        {
            RestaurantId = restaurant.Id,
            TableNumber = 8,
            Name = "Masa 8",
            QrToken = "table-token",
            IsActive = true
        });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task CreateOrder_uses_database_price_not_client_price()
    {
        await using var db = CreateDb();
        var orders = new OrderManager(db, new MenuManager(db));
        var tableId = db.RestaurantTables.Single().Id;
        var productId = db.Products.Single().Id;

        var result = await orders.CreateOrderAsync(tableId, [new CartLineInput { ProductId = productId, Quantity = 2 }], null);

        Assert.True(result.Success);
        Assert.Equal(440m, result.Data!.TotalAmount);
        Assert.Equal(220m, result.Data.Items.Single().UnitPrice);
    }

    [Fact]
    public async Task CreateOrder_rejects_zero_quantity()
    {
        await using var db = CreateDb();
        var orders = new OrderManager(db, new MenuManager(db));
        var tableId = db.RestaurantTables.Single().Id;
        var productId = db.Products.Single().Id;

        var result = await orders.CreateOrderAsync(tableId, [new CartLineInput { ProductId = productId, Quantity = 0 }], null);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateOrder_rejects_unavailable_product()
    {
        await using var db = CreateDb();
        var product = db.Products.Single();
        product.IsAvailable = false;
        await db.SaveChangesAsync();
        var orders = new OrderManager(db, new MenuManager(db));

        var result = await orders.CreateOrderAsync(
            db.RestaurantTables.Single().Id,
            [new CartLineInput { ProductId = product.Id, Quantity = 1 }],
            null);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Changing_product_price_does_not_change_existing_order()
    {
        await using var db = CreateDb();
        var orders = new OrderManager(db, new MenuManager(db));
        var created = await orders.CreateOrderAsync(
            db.RestaurantTables.Single().Id,
            [new CartLineInput { ProductId = db.Products.Single().Id, Quantity = 1 }],
            null);
        var product = db.Products.Single();
        product.Price = 999m;
        await db.SaveChangesAsync();

        var loaded = await orders.GetByIdAsync(created.Data!.Id);
        Assert.Equal(220m, loaded!.TotalAmount);
    }

    [Fact]
    public async Task Invalid_status_skip_is_rejected()
    {
        await using var db = CreateDb();
        var orders = new OrderManager(db, new MenuManager(db));
        var created = await orders.CreateOrderAsync(
            db.RestaurantTables.Single().Id,
            [new CartLineInput { ProductId = db.Products.Single().Id, Quantity = 1 }],
            null);

        var result = await orders.ChangeStatusAsync(created.Data!.Id, OrderStatus.Ready);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task PlaceCustomerOrder_with_valid_qr_goes_to_kitchen()
    {
        await using var db = CreateDb();
        var orders = new OrderManager(db, new MenuManager(db));
        var productId = db.Products.Single().Id;

        var placed = await orders.PlaceCustomerOrderAsync(
            "test-rest",
            "table-token",
            [new CartLineInput { ProductId = productId, Quantity = 1 }],
            null);

        Assert.True(placed.Success);
        Assert.Equal(OrderStatus.New, placed.Data!.Status);

        var kitchen = await orders.GetKitchenOrdersAsync();
        Assert.Contains(kitchen, o => o.Id == placed.Data.Id);
    }

    [Fact]
    public async Task PlaceCustomerOrder_with_invalid_qr_is_rejected()
    {
        await using var db = CreateDb();
        var orders = new OrderManager(db, new MenuManager(db));
        var productId = db.Products.Single().Id;

        var placed = await orders.PlaceCustomerOrderAsync(
            "yanlis",
            "token",
            [new CartLineInput { ProductId = productId, Quantity = 1 }],
            null);

        Assert.False(placed.Success);
        Assert.Empty(await orders.GetKitchenOrdersAsync());
    }

    [Fact]
    public async Task CreateOrder_rejects_product_from_another_restaurant()
    {
        await using var db = CreateDb();
        var other = new Restaurant { Name = "Diğer", PublicToken = "other", IsActive = true };
        db.Restaurants.Add(other);
        await db.SaveChangesAsync();
        var category = new Category { RestaurantId = other.Id, Name = "X", DisplayOrder = 1, IsActive = true };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var foreign = new Product { CategoryId = category.Id, Name = "Yabancı", Price = 10m, IsActive = true, IsAvailable = true };
        db.Products.Add(foreign);
        await db.SaveChangesAsync();

        var orders = new OrderManager(db, new MenuManager(db));
        var result = await orders.CreateOrderAsync(
            db.RestaurantTables.Single(t => t.QrToken == "table-token").Id,
            [new CartLineInput { ProductId = foreign.Id, Quantity = 1 }],
            null);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Order_numbers_are_unique()
    {
        await using var db = CreateDb();
        var orders = new OrderManager(db, new MenuManager(db));
        var tableId = db.RestaurantTables.Single().Id;
        var productId = db.Products.Single().Id;

        var first = await orders.CreateOrderAsync(tableId, [new CartLineInput { ProductId = productId, Quantity = 1 }], null);
        var second = await orders.CreateOrderAsync(tableId, [new CartLineInput { ProductId = productId, Quantity = 1 }], null);

        Assert.True(first.Success && second.Success);
        Assert.NotEqual(first.Data!.OrderNumber, second.Data!.OrderNumber);
    }

    [Fact]
    public async Task Long_customer_note_is_clipped()
    {
        await using var db = CreateDb();
        var orders = new OrderManager(db, new MenuManager(db));
        var note = new string('a', 800);
        var result = await orders.CreateOrderAsync(
            db.RestaurantTables.Single().Id,
            [new CartLineInput { ProductId = db.Products.Single().Id, Quantity = 1, Note = new string('b', 400) }],
            note);

        Assert.True(result.Success);
        Assert.Equal(IOrderService.MaxNoteLength, result.Data!.CustomerNote!.Length);
        Assert.Equal(IOrderService.MaxLineNoteLength, result.Data.Items.Single().Note!.Length);
    }
}

public class ServiceRequestMachineTests
{
    [Fact]
    public void Completed_service_request_cannot_go_back()
    {
        Assert.False(ServiceRequestMachine.CanTransition(ServiceRequestStatus.Completed, ServiceRequestStatus.Pending));
        Assert.True(ServiceRequestMachine.CanTransition(ServiceRequestStatus.Pending, ServiceRequestStatus.Acknowledged));
    }
}

public class MenuManagerTests
{
    [Fact]
    public async Task Invalid_qr_token_is_rejected()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var menu = new MenuManager(db);
        var result = await menu.ResolveTableAsync("x", "y");
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Inactive_table_is_rejected()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        db.Restaurants.Add(new Restaurant { Name = "R", PublicToken = "r1", IsActive = true });
        await db.SaveChangesAsync();
        db.RestaurantTables.Add(new RestaurantTable
        {
            RestaurantId = 1,
            TableNumber = 1,
            Name = "Masa 1",
            QrToken = "tok",
            IsActive = false
        });
        await db.SaveChangesAsync();
        var menu = new MenuManager(db);
        var result = await menu.ResolveTableAsync("r1", "tok");
        Assert.False(result.Success);
    }
}
