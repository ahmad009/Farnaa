using Microsoft.EntityFrameworkCore;
using Ordering.Application;
using Ordering.Domain;

namespace Ordering.Infrastructure;

internal sealed class OrderRepository(IDbContextFactory<OrderingDbContext> factory) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Order?> FindByReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Orders.AsNoTracking().Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken);
    }
}
