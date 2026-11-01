using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application;
using Ordering.Contracts;

namespace Ordering.Infrastructure;

public static class OrderingModule
{
    public static IServiceCollection AddOrdering(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<OrderingDbContext>(options => options.UseSqlServer(connectionString,
            sql => sql.CommandTimeout(30)));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPlaceOrder, PlaceOrder>();
        return services;
    }

    public static async Task InitializeOrderingAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OrderingDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}

public sealed class OrderingDesignFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<OrderingDbContext>()
        .UseSqlServer("Server=.;Database=OrderingDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True").Options);
}
