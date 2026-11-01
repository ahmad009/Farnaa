using Catalog.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

internal sealed class SalesRulesProvider(IDbContextFactory<CatalogDbContext> factory) : ISalesRulesProvider
{
    public async Task<SalesRules> GetAsync(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var settings = await db.SalesSettings.AsNoTracking().SingleAsync(cancellationToken);
        settings.EnsureValid();
        return new(settings.MaxOrderItems, settings.MaxItemQuantity, settings.MaxUnitPrice);
    }
}
