using Catalog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Ordering.Infrastructure;

namespace Marketplace.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string suffix = Guid.NewGuid().ToString("N");
    private string? catalogConnection;
    private string? orderingConnection;
    public Action<IServiceCollection>? ConfigureServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var secrets = new ConfigurationBuilder().AddUserSecrets<Program>(optional: true).Build();
        var baseConnection = Environment.GetEnvironmentVariable("MARKETPLACE_TEST_CONNECTION")
            ?? secrets.GetConnectionString("CatalogDb")
            ?? "Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
        catalogConnection = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = $"MarketplaceTests_Catalog_{suffix}" }.ToString();
        orderingConnection = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = $"MarketplaceTests_Ordering_{suffix}" }.ToString();
        // Create through master first, avoiding failed login attempts to nonexistent
        // databases and SqlClient's resulting connection-pool blocking period.
        using (var master = new SqlConnection(new SqlConnectionStringBuilder(baseConnection)
               { InitialCatalog = "master", Pooling = false }.ToString()))
        {
            master.Open();
            foreach (var name in new[] { $"MarketplaceTests_Catalog_{suffix}", $"MarketplaceTests_Ordering_{suffix}" })
            {
                using var command = master.CreateCommand();
                command.CommandText = $"IF DB_ID(@name) IS NULL CREATE DATABASE [{name}];";
                command.Parameters.AddWithValue("@name", name);
                command.ExecuteNonQuery();
            }
        }
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:CatalogDb"] = catalogConnection,
            ["ConnectionStrings:OrderingDb"] = orderingConnection
        }));
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            // Replace module registrations so every context uses the test databases.
            services.RemoveAll<IDbContextFactory<CatalogDbContext>>();
            services.RemoveAll<DbContextOptions<CatalogDbContext>>();
            services.RemoveAll<IDbContextFactory<OrderingDbContext>>();
            services.RemoveAll<DbContextOptions<OrderingDbContext>>();
            services.AddDbContextFactory<CatalogDbContext>(options => options.UseSqlServer(catalogConnection));
            services.AddDbContextFactory<OrderingDbContext>(options => options.UseSqlServer(orderingConnection));
            ConfigureServices?.Invoke(services);
        });
    }

    public new HttpClient CreateClient()
    {
        var client = base.CreateClient();
        using var catalog = Services.GetRequiredService<IDbContextFactory<CatalogDbContext>>().CreateDbContext();
        using var ordering = Services.GetRequiredService<IDbContextFactory<OrderingDbContext>>().CreateDbContext();
        if (catalog.Database.GetDbConnection().Database != $"MarketplaceTests_Catalog_{suffix}" ||
            ordering.Database.GetDbConnection().Database != $"MarketplaceTests_Ordering_{suffix}")
        {
            client.Dispose();
            throw new InvalidOperationException("Test database isolation failed; no HTTP request is permitted.");
        }
        return client;
    }

    public async Task<CatalogDbContext> CatalogAsync() => await Services
        .GetRequiredService<IDbContextFactory<CatalogDbContext>>().CreateDbContextAsync();
    public async Task<OrderingDbContext> OrderingAsync() => await Services
        .GetRequiredService<IDbContextFactory<OrderingDbContext>>().CreateDbContextAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        foreach (var connection in new[] { catalogConnection, orderingConnection }.OfType<string>())
        {
            var settings = new SqlConnectionStringBuilder(connection);
            if (!settings.InitialCatalog.StartsWith("MarketplaceTests_", StringComparison.Ordinal) || !settings.InitialCatalog.EndsWith(suffix, StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to drop a database outside this test's ownership.");
            var database = settings.InitialCatalog;
            using var pooled = new SqlConnection(connection);
            SqlConnection.ClearPool(pooled);
            settings.InitialCatalog = "master";
            settings.Pooling = false;
            await using var master = new SqlConnection(settings.ToString());
            await master.OpenAsync();
            await using var command = master.CreateCommand();
            command.CommandText = $"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END";
            command.Parameters.AddWithValue("@name", database);
            await command.ExecuteNonQueryAsync();
        }
    }
}
