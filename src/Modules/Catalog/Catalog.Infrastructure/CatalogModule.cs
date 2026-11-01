using Catalog.Application;
using Catalog.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

public static class CatalogModule
{
    public static IServiceCollection AddCatalog(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<CatalogDbContext>(options => options.UseSqlServer(connectionString,
            sql => sql.CommandTimeout(30)));
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ICatalogInventory, InventoryService>();
        services.AddScoped<ISalesRulesProvider, SalesRulesProvider>();
        return services;
    }

    public static async Task InitializeCatalogAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CatalogDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}

public sealed class CatalogDesignFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<CatalogDbContext>()
        .UseSqlServer("Server=.;Database=CatalogDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True").Options);
}
