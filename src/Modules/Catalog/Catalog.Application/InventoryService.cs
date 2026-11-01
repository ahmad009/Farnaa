using Catalog.Contracts;

namespace Catalog.Application;

public sealed class InventoryService(IInventoryRepository repository, ISalesRulesProvider salesRules) : ICatalogInventory
{
    public async Task<Reservation> ReserveAsync(IReadOnlyList<StockRequest> items, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        var rules = await salesRules.GetAsync(cancellationToken);
        if (items.Count == 0 || items.Count > rules.MaxOrderItems || items.Any(x => x is null || x.SellingOptionId == Guid.Empty || x.Quantity <= 0 || x.Quantity > rules.MaxItemQuantity)
            || items.Select(x => x.SellingOptionId).Distinct().Count() != items.Count)
            throw new InventoryException("invalid_order", "Items do not satisfy the current sales limits.");
        return await repository.ReserveAsync(items, rules.MaxUnitPrice, cancellationToken);
    }

    public Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken) =>
        repository.ReleaseAsync(reservationId, cancellationToken);
}
