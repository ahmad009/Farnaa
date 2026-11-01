namespace Catalog.Contracts;

public sealed record SalesRules(int MaxOrderItems, int MaxItemQuantity, decimal MaxUnitPrice);

public interface ISalesRulesProvider
{
    Task<SalesRules> GetAsync(CancellationToken cancellationToken);
}
