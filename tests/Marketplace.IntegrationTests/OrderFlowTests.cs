using System.Net;
using System.Net.Http.Json;
using Catalog.Contracts;
using Catalog.Domain;
using Catalog.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ordering.Application;
using Ordering.Contracts;
using Ordering.Domain;
using Ordering.Infrastructure;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Marketplace.IntegrationTests;

public sealed class OrderFlowTests
{
    private static readonly Guid Customer = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static PlaceOrderRequest Request(Guid? option = null, int quantity = 1) =>
        new(Customer, [new(option ?? SeedIds.OptionOne, quantity)]);

    [Theory]
    [InlineData(5)]
    [InlineData(20)]
    public async Task Concurrent_buyers_of_last_unit_create_exactly_one_order(int buyers)
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, buyers).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            return await client.PostAsJsonAsync("/api/orders", Request());
        })).ToArray();
        start.SetResult();
        var responses = await Task.WhenAll(tasks);
        try
        {
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
            Assert.Equal(buyers - 1, responses.Count(x => x.StatusCode == HttpStatusCode.Conflict));
            await using var catalog = await app.CatalogAsync();
            await using var ordering = await app.OrderingAsync();
            Assert.Equal(0, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
            Assert.Equal(1, await ordering.Orders.CountAsync());
            Assert.Equal(1, await catalog.Reservations.CountAsync(x => !x.Released));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Fact]
    public async Task Seed_contains_shahram_bahram_titap_and_rangarang()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await using var catalog = await app.CatalogAsync();
        var products = await catalog.Set<Product>().ToDictionaryAsync(x => x.Id);
        var sellers = await catalog.Set<Seller>().ToDictionaryAsync(x => x.Id);
        Assert.Equal(2, products.Count);
        Assert.Equal("تیتاپ", products[SeedIds.Product].Name);
        Assert.Equal("KB-001", products[SeedIds.Product].SKU);
        Assert.Equal("رنگارنگ", products[SeedIds.ProductTwo].Name);
        Assert.Equal("KB-002", products[SeedIds.ProductTwo].SKU);
        Assert.Equal("شهرام", sellers[SeedIds.SellerOne].Title);
        Assert.Equal("بهرام", sellers[SeedIds.SellerTwo].Title);
    }

    [Fact]
    public async Task Separate_hosts_have_isolated_test_data()
    {
        await using var first = new ApiFactory();
        await using var second = new ApiFactory();
        using var firstClient = first.CreateClient();
        using var secondClient = second.CreateClient();
        using var response = await firstClient.PostAsJsonAsync("/api/orders", Request());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var firstCatalog = await first.CatalogAsync();
        await using var secondCatalog = await second.CatalogAsync();
        await using var secondOrdering = await second.OrderingAsync();
        Assert.NotEqual(firstCatalog.Database.GetDbConnection().Database, secondCatalog.Database.GetDbConnection().Database);
        Assert.Equal(1, await secondCatalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
        Assert.Equal(0, await secondOrdering.Orders.CountAsync());
    }

    [Fact]
    public async Task Successful_order_snapshots_price_and_persists_items()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest(Customer,
            [new(SeedIds.OptionOne, 1), new(SeedIds.OptionTwo, 2)]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(body);
        Assert.Equal(385.50m, body.TotalAmount);
        Assert.Equal("Placed", body.Status);
        await using var catalog = await app.CatalogAsync();
        await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Price, 999m));
        await using var ordering = await app.OrderingAsync();
        var saved = await ordering.Orders.Include(x => x.Items).SingleAsync();
        Assert.Equal(385.50m, saved.TotalAmount);
        Assert.Equal(125.50m, saved.Items.Single(x => x.SellingOptionId == SeedIds.OptionOne).UnitPrice);
        Assert.Equal(2, saved.Items.Count);
        Assert.Equal(8, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionTwo).Select(x => x.StockQuantity).SingleAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_later_item_rolls_back_all_earlier_stock_changes(bool missing)
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        // This missing ID sorts after both seed IDs, so an earlier UPDATE executes first.
        var second = missing ? Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff") : SeedIds.OptionTwo;
        using var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest(Customer,
            [new(SeedIds.OptionOne, 1), new(second, 11)]));
        Assert.Equal(missing ? HttpStatusCode.NotFound : HttpStatusCode.Conflict, response.StatusCode);
        await using var catalog = await app.CatalogAsync();
        Assert.Equal(1, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
        Assert.Equal(10, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionTwo).Select(x => x.StockQuantity).SingleAsync());
        Assert.Equal(0, await catalog.Reservations.CountAsync());
        await using var ordering = await app.OrderingAsync();
        Assert.Equal(0, await ordering.Orders.CountAsync());
    }

    [Fact]
    public async Task Database_failure_compensates_all_items_and_release_is_idempotent()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await using (var ordering = await app.OrderingAsync())
            await ordering.Database.ExecuteSqlRawAsync("""
                ALTER TABLE dbo.OrderItems ADD CONSTRAINT CK_Test_RejectItems CHECK (Quantity < 0);
                """);
        using var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest(Customer,
            [new(SeedIds.OptionOne, 1), new(SeedIds.OptionTwo, 3)]));
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await using var catalog = await app.CatalogAsync();
        var reservation = await catalog.Reservations.SingleAsync();
        Assert.True(reservation.Released);
        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Task.Run(async () =>
        {
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ICatalogInventory>().ReleaseAsync(reservation.Id, CancellationToken.None);
        })));
        Assert.Equal(1, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
        Assert.Equal(10, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionTwo).Select(x => x.StockQuantity).SingleAsync());
        await using var check = await app.OrderingAsync();
        Assert.Equal(0, await check.Orders.CountAsync());
        Assert.Equal(0, await check.Set<OrderItem>().CountAsync());
    }

    [Fact]
    public async Task Lost_commit_acknowledgment_does_not_release_sold_inventory()
    {
        await using var app = new ApiFactory
        {
            ConfigureServices = services => services.Replace(ServiceDescriptor.Scoped<IOrderRepository, CommitThenThrowRepository>())
        };
        using var client = app.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/orders", Request());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var catalog = await app.CatalogAsync();
        Assert.Equal(0, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
        Assert.False((await catalog.Reservations.SingleAsync()).Released);
        await using var ordering = await app.OrderingAsync();
        Assert.Equal(1, await ordering.Orders.CountAsync());
    }

    [Fact]
    public async Task Unavailable_order_outcome_keeps_inventory_reserved()
    {
        await using var app = new ApiFactory
        {
            ConfigureServices = services => services.Replace(ServiceDescriptor.Scoped<IOrderRepository, UnknownOutcomeRepository>())
        };
        using var client = app.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/orders", Request());
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await using var catalog = await app.CatalogAsync();
        Assert.Equal(0, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
        Assert.False((await catalog.Reservations.SingleAsync()).Released);
    }

    [Fact]
    public async Task Compensation_failure_rolls_back_release_marker_and_can_be_retried()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await using (var ordering = await app.OrderingAsync())
            await ordering.Database.ExecuteSqlRawAsync("""
                ALTER TABLE dbo.OrderItems ADD CONSTRAINT CK_Test_RejectItems CHECK (Quantity < 0);
                """);
        await using (var setup = await app.CatalogAsync())
            await setup.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER FailRelease ON dbo.SellingOptions AFTER UPDATE AS
                BEGIN
                    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE i.StockQuantity > d.StockQuantity)
                        THROW 51001, 'injected compensation failure', 1;
                END;
                """);
        using var response = await client.PostAsJsonAsync("/api/orders", Request());
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await using var catalog = await app.CatalogAsync();
        var reservation = await catalog.Reservations.AsNoTracking().SingleAsync();
        Assert.False(reservation.Released);
        Assert.Equal(0, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
        await catalog.Database.ExecuteSqlRawAsync("DROP TRIGGER FailRelease;");
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ICatalogInventory>().ReleaseAsync(reservation.Id, CancellationToken.None);
        Assert.True((await catalog.Reservations.AsNoTracking().SingleAsync()).Released);
        Assert.Equal(1, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
    }

    [Fact]
    public async Task Cancellation_after_reservation_still_compensates()
    {
        using var cancellation = new CancellationTokenSource();
        await using var app = new ApiFactory
        {
            ConfigureServices = services =>
            {
                services.AddSingleton(cancellation);
                services.Replace(ServiceDescriptor.Scoped<IOrderRepository, CancelRepository>());
            }
        };
        using var client = app.CreateClient();
        await using var scope = app.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IPlaceOrder>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExecuteAsync(Request(), cancellation.Token));
        Assert.True(cancellation.IsCancellationRequested);
        await using var catalog = await app.CatalogAsync();
        Assert.Equal(1, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
        Assert.True((await catalog.Reservations.SingleAsync()).Released);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100001)]
    public async Task Invalid_quantity_is_rejected_without_changes(int quantity)
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/orders", Request(quantity: quantity));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var catalog = await app.CatalogAsync();
        Assert.Equal(0, await catalog.Reservations.CountAsync());
    }

    [Theory]
    [InlineData("{\"customerId\":\"40000000-0000-0000-0000-000000000001\",\"items\":null}")]
    [InlineData("{\"customerId\":\"40000000-0000-0000-0000-000000000001\",\"items\":[]}")]
    [InlineData("{\"customerId\":\"40000000-0000-0000-0000-000000000001\",\"items\":[null]}")]
    [InlineData("{\"customerId\":\"00000000-0000-0000-0000-000000000000\",\"items\":[]}")]
    [InlineData("{\"customerId\":\"not-a-guid\",\"items\":[]}")]
    [InlineData("{")]
    public async Task Invalid_json_or_items_return_bad_request(string json)
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        using var response = await client.PostAsync("/api/orders", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_items_are_rejected()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest(Customer,
            [new(SeedIds.OptionOne, 1), new(SeedIds.OptionOne, 1)]));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var catalog = await app.CatalogAsync();
        Assert.Equal(1, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
    }

    [Fact]
    public async Task Database_enforces_unique_seller_product_and_nonnegative_stock()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await using var db = await app.CatalogAsync();
        var duplicate = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO SellingOptions (Id, ProductId, SellerId, Price, StockQuantity)
            VALUES ({Guid.NewGuid()}, {SeedIds.Product}, {SeedIds.SellerOne}, 100, 1)
            """));
        Assert.Contains(duplicate.Number, new[] { 2601, 2627 });
        var negative = await Assert.ThrowsAsync<SqlException>(() => db.SellingOptions
            .Where(x => x.Id == SeedIds.OptionOne).ExecuteUpdateAsync(s => s.SetProperty(x => x.StockQuantity, -1)));
        Assert.Equal(547, negative.Number);
    }

    [Fact]
    public async Task Quantity_limit_changes_take_effect_without_restart()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await using var catalog = await app.CatalogAsync();
        await catalog.SalesSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.MaxItemQuantity, 1));
        using var rejected = await client.PostAsJsonAsync("/api/orders", Request(SeedIds.OptionTwo, 2));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Empty(await catalog.Reservations.ToListAsync());

        await catalog.SalesSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.MaxItemQuantity, 2));
        using var accepted = await client.PostAsJsonAsync("/api/orders", Request(SeedIds.OptionTwo, 2));
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);

        await catalog.SalesSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.MaxItemQuantity, 100_001));
        await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionTwo).ExecuteUpdateAsync(s => s.SetProperty(x => x.StockQuantity, 100_001));
        using var aboveOldLimit = await client.PostAsJsonAsync("/api/orders", Request(SeedIds.OptionTwo, 100_001));
        Assert.Equal(HttpStatusCode.Created, aboveOldLimit.StatusCode);
    }

    [Fact]
    public async Task Item_count_limit_is_read_from_database()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await using var catalog = await app.CatalogAsync();
        var request = new PlaceOrderRequest(Customer, [new(SeedIds.OptionOne, 1), new(SeedIds.OptionTwo, 1)]);
        await catalog.SalesSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.MaxOrderItems, 1));
        using var rejected = await client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        await catalog.SalesSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.MaxOrderItems, 2));
        using var accepted = await client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
    }

    [Fact]
    public async Task Price_limit_rejects_purchase_and_can_exceed_the_old_fixed_ceiling()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await using var catalog = await app.CatalogAsync();
        await catalog.SalesSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.MaxUnitPrice, 120m));
        using var rejected = await client.PostAsJsonAsync("/api/orders", Request());
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal(1, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
        Assert.Empty(await catalog.Reservations.ToListAsync());

        await catalog.SalesSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.MaxUnitPrice, 1_000_000_002m));
        await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).ExecuteUpdateAsync(s => s.SetProperty(x => x.Price, 1_000_000_001m));
        using var accepted = await client.PostAsJsonAsync("/api/orders", Request());
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        var order = await accepted.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(1_000_000_001m, order!.TotalAmount);
    }

    [Fact]
    public async Task Unsafe_settings_reject_orders_before_reserving_stock()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await using var catalog = await app.CatalogAsync();
        await catalog.SalesSettings.ExecuteUpdateAsync(s => s
            .SetProperty(x => x.MaxOrderItems, int.MaxValue)
            .SetProperty(x => x.MaxItemQuantity, int.MaxValue));
        using var response = await client.PostAsJsonAsync("/api/orders", Request());
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Empty(await catalog.Reservations.ToListAsync());
        Assert.Equal(1, await catalog.SellingOptions.Where(x => x.Id == SeedIds.OptionOne).Select(x => x.StockQuantity).SingleAsync());
    }

    [Fact]
    public async Task Both_databases_have_migrations_and_no_pending_model_changes()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        await using var catalog = await app.CatalogAsync();
        await using var ordering = await app.OrderingAsync();
        Assert.Single(await catalog.Database.GetAppliedMigrationsAsync());
        Assert.Single(await ordering.Database.GetAppliedMigrationsAsync());
        Assert.False(catalog.Database.HasPendingModelChanges());
        Assert.False(ordering.Database.HasPendingModelChanges());
        Assert.NotEqual(catalog.Database.GetDbConnection().Database, ordering.Database.GetDbConnection().Database);
    }

    private sealed class CommitThenThrowRepository(IDbContextFactory<OrderingDbContext> factory) : IOrderRepository
    {
        public async Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken);
            throw new IOException("Simulated lost commit acknowledgment.");
        }
        public async Task<Order?> FindByReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            return await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken);
        }
    }

    private sealed class UnknownOutcomeRepository : IOrderRepository
    {
        public Task AddAsync(Order order, CancellationToken cancellationToken) => throw new IOException("Storage unavailable.");
        public Task<Order?> FindByReservationAsync(Guid reservationId, CancellationToken cancellationToken) => throw new IOException("Cannot determine commit outcome.");
    }

    private sealed class CancelRepository(CancellationTokenSource source) : IOrderRepository
    {
        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            source.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Expected caller cancellation.");
        }
        public Task<Order?> FindByReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            Assert.False(cancellationToken.IsCancellationRequested);
            return Task.FromResult<Order?>(null);
        }
    }
}

