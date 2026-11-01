using Ordering.Domain;

namespace Ordering.Application;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    // Use a fresh context: the failed write may still have committed.
    Task<Order?> FindByReservationAsync(Guid reservationId, CancellationToken cancellationToken);
}
