namespace Catalog.Contracts;

public sealed record StockRequest(Guid SellingOptionId, int Quantity);
public sealed record ReservedItem(Guid SellingOptionId, int Quantity, decimal UnitPrice);
public sealed record Reservation(Guid Id, IReadOnlyList<ReservedItem> Items);

public interface ICatalogInventory
{
    Task<Reservation> ReserveAsync(IReadOnlyList<StockRequest> items, CancellationToken cancellationToken);
    Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken);
}

public sealed class InventoryException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
