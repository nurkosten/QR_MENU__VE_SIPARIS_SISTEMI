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
        Assert.False(OrderStatusMachine.CanTransition(OrderStatus.New, OrderStatus.Preparing));
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
        var restaurant = new Restaurant { Name = "Test", PublicToken = "test-rest", MenuQrToken = "table-token", IsActive = true };
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
    public async Task PlaceCustomerOrder_stays_with_staff_until_confirmed()
    {
        await using var db = CreateDb();
        var orders = new OrderManager(db, new MenuManager(db));
        var restaurantId = db.Restaurants.Select(r => r.Id).Single();
        var productId = db.Products.Single().Id;

        var placed = await orders.PlaceCustomerOrderAsync(
            "test-rest",
            "table-token",
            [new CartLineInput { ProductId = productId, Quantity = 1 }],
            null);

        Assert.True(placed.Success);
        Assert.Equal(OrderStatus.New, placed.Data!.Status);
        Assert.DoesNotContain(await orders.GetKitchenOrdersAsync(restaurantId), o => o.Id == placed.Data.Id);

        var confirmed = await orders.ChangeStatusAsync(placed.Data.Id, OrderStatus.Confirmed);
        Assert.True(confirmed.Success);
        Assert.Contains(await orders.GetKitchenOrdersAsync(restaurantId), o => o.Id == placed.Data.Id);
    }

    [Fact]
    public async Task Kitchen_does_not_show_served_or_completed_orders()
    {
        await using var db = CreateDb();
        var orders = new OrderManager(db, new MenuManager(db));
        var restaurantId = db.Restaurants.Select(r => r.Id).Single();
        var created = await orders.CreateOrderAsync(
            db.RestaurantTables.Single().Id,
            [new CartLineInput { ProductId = db.Products.Single().Id, Quantity = 1 }],
            null);

        Assert.True((await orders.ChangeStatusAsync(created.Data!.Id, OrderStatus.Confirmed)).Success);
        Assert.True((await orders.ChangeStatusAsync(created.Data.Id, OrderStatus.Preparing)).Success);
        Assert.True((await orders.ChangeStatusAsync(created.Data.Id, OrderStatus.Ready)).Success);
        Assert.Contains(await orders.GetKitchenOrdersAsync(restaurantId), o => o.Id == created.Data.Id);

        Assert.True((await orders.ChangeStatusAsync(created.Data.Id, OrderStatus.Served)).Success);
        Assert.DoesNotContain(await orders.GetKitchenOrdersAsync(restaurantId), o => o.Id == created.Data.Id);
        Assert.DoesNotContain(await orders.GetStaffOrdersAsync(restaurantId), o => o.Id == created.Data.Id);
        Assert.Contains(await orders.GetPastOrdersAsync(restaurantId), o => o.Id == created.Data.Id && o.Status == OrderStatus.Served);

        Assert.True((await orders.ChangeStatusAsync(created.Data.Id, OrderStatus.Completed)).Success);
        Assert.DoesNotContain(await orders.GetKitchenOrdersAsync(restaurantId), o => o.Id == created.Data.Id);
        Assert.DoesNotContain(await orders.GetStaffOrdersAsync(restaurantId), o => o.Id == created.Data.Id);
        Assert.Contains(await orders.GetPastOrdersAsync(restaurantId), o => o.Id == created.Data.Id && o.Status == OrderStatus.Completed);
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
        Assert.Empty(await orders.GetKitchenOrdersAsync(db.Restaurants.Select(r => r.Id).Single()));
    }

    [Fact]
    public async Task PlaceCustomerOrder_rejects_other_table_id()
    {
        await using var db = CreateDb();
        var restaurantId = db.Restaurants.Select(r => r.Id).Single();
        db.RestaurantTables.Add(new RestaurantTable
        {
            RestaurantId = restaurantId,
            TableNumber = 9,
            Name = "Masa 9",
            QrToken = "other-table",
            IsActive = true
        });
        await db.SaveChangesAsync();
        var otherId = db.RestaurantTables.Single(t => t.QrToken == "other-table").Id;
        var orders = new OrderManager(db, new MenuManager(db));

        var placed = await orders.PlaceCustomerOrderAsync(
            "test-rest",
            "table-token",
            [new CartLineInput { ProductId = db.Products.Single().Id, Quantity = 1 }],
            null,
            otherId);

        Assert.False(placed.Success);
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task CreateOrder_rejects_product_from_another_restaurant()
    {
        await using var db = CreateDb();
        var other = new Restaurant { Name = "Diğer", PublicToken = "other", MenuQrToken = "other-qr", IsActive = true };
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

    [Fact]
    public async Task Kitchen_dashboard_and_staff_lists_are_scoped_to_restaurant()
    {
        await using var db = CreateDb();
        var firstId = db.Restaurants.Select(r => r.Id).Single();
        var orders = new OrderManager(db, new MenuManager(db));
        var reports = new ReportManager(db);

        var firstCreated = await orders.CreateOrderAsync(
            db.RestaurantTables.Single().Id,
            [new CartLineInput { ProductId = db.Products.Single().Id, Quantity = 1 }],
            null);
        Assert.True(firstCreated.Success);
        await orders.ChangeStatusAsync(firstCreated.Data!.Id, OrderStatus.Confirmed);

        var other = new Restaurant { Name = "Diğer", PublicToken = "other-rest", MenuQrToken = "other-shared", IsActive = true };
        db.Restaurants.Add(other);
        await db.SaveChangesAsync();
        var otherCategory = new Category { RestaurantId = other.Id, Name = "Çorba", DisplayOrder = 1, IsActive = true };
        db.Categories.Add(otherCategory);
        await db.SaveChangesAsync();
        var otherProduct = new Product { CategoryId = otherCategory.Id, Name = "Mercimek", Price = 80m, IsActive = true, IsAvailable = true };
        db.Products.Add(otherProduct);
        db.RestaurantTables.Add(new RestaurantTable
        {
            RestaurantId = other.Id,
            TableNumber = 1,
            Name = "Masa 1",
            QrToken = "other-shared",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var otherCreated = await orders.CreateOrderAsync(
            db.RestaurantTables.Single(t => t.RestaurantId == other.Id).Id,
            [new CartLineInput { ProductId = otherProduct.Id, Quantity = 1 }],
            null);
        Assert.True(otherCreated.Success);
        await orders.ChangeStatusAsync(otherCreated.Data!.Id, OrderStatus.Confirmed);

        var firstKitchen = await orders.GetKitchenOrdersAsync(firstId);
        var otherKitchen = await orders.GetKitchenOrdersAsync(other.Id);
        Assert.Single(firstKitchen);
        Assert.Single(otherKitchen);
        Assert.Equal(firstId, firstKitchen.Single().Table.RestaurantId);
        Assert.Equal(other.Id, otherKitchen.Single().Table.RestaurantId);

        var firstDash = await reports.GetDashboardAsync(firstId);
        var otherDash = await reports.GetDashboardAsync(other.Id);
        Assert.Equal(1, firstDash.OpenOrderCount);
        Assert.Equal(1, otherDash.OpenOrderCount);
        Assert.Equal(1, firstDash.ActiveTableCount);
        Assert.Equal(1, otherDash.ActiveTableCount);
        Assert.Equal(1, firstDash.AvailableProductCount);
        Assert.Equal(1, otherDash.AvailableProductCount);

        var firstStaff = await orders.GetStaffOrdersAsync(firstId);
        var otherAdmin = await orders.GetAdminOrdersAsync(other.Id, null, null, null);
        Assert.Single(firstStaff);
        Assert.Equal(firstId, firstStaff.Single().Table.RestaurantId);
        Assert.Single(otherAdmin);
        Assert.Equal(other.Id, otherAdmin.Single().Table.RestaurantId);

        var elsewhere = await orders.GetStaffWorkElsewhereAsync(other.Id);
        Assert.Single(elsewhere);
        Assert.Equal(firstId, elsewhere.Single().RestaurantId);
        Assert.Equal(1, elsewhere.Single().Count);
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
        db.Restaurants.Add(new Restaurant { Name = "R", PublicToken = "r1", MenuQrToken = "tok", IsActive = true });
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

    [Fact]
    public async Task Table_qr_binds_only_that_table()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        db.Restaurants.Add(new Restaurant { Name = "R", PublicToken = "r1", MenuQrToken = "rest", IsActive = true });
        await db.SaveChangesAsync();
        db.RestaurantTables.AddRange(
            new RestaurantTable { RestaurantId = 1, TableNumber = 1, Name = "Masa 1", QrToken = "qr-a", IsActive = true },
            new RestaurantTable { RestaurantId = 1, TableNumber = 2, Name = "Masa 2", QrToken = "qr-b", IsActive = true });
        await db.SaveChangesAsync();

        var menu = new MenuManager(db);
        var first = await menu.ResolveTableAsync("r1", "qr-a");
        var second = await menu.ResolveTableAsync("r1", "qr-b");

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(1, first.Data!.Table.TableNumber);
        Assert.Equal(2, second.Data!.Table.TableNumber);
    }

    [Fact]
    public async Task Wrong_table_id_with_another_tables_qr_is_rejected()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        db.Restaurants.Add(new Restaurant { Name = "R", PublicToken = "r1", MenuQrToken = "rest", IsActive = true });
        await db.SaveChangesAsync();
        db.RestaurantTables.AddRange(
            new RestaurantTable { RestaurantId = 1, TableNumber = 1, Name = "Masa 1", QrToken = "qr-a", IsActive = true },
            new RestaurantTable { RestaurantId = 1, TableNumber = 2, Name = "Masa 2", QrToken = "qr-b", IsActive = true });
        await db.SaveChangesAsync();

        var menu = new MenuManager(db);
        var stolen = await menu.ResolveTableAsync("r1", "qr-a", 2);
        var page = await menu.GetMenuAsync("r1", "qr-a");

        Assert.False(stolen.Success);
        Assert.True(page.Success);
        Assert.NotNull(page.Data!.Table);
        Assert.Equal(1, page.Data.Table.TableNumber);
        Assert.Single(page.Data.ActiveTables);
    }
}
