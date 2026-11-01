using Catalog.Contracts;

namespace Catalog.Application;

public interface IInventoryRepository
{
    Task<Reservation> ReserveAsync(IReadOnlyList<StockRequest> items, decimal maxUnitPrice, CancellationToken cancellationToken);
    Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken);
}
