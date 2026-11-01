using Catalog.Application;
using Catalog.Contracts;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

internal sealed class InventoryRepository(IDbContextFactory<CatalogDbContext> factory, TimeProvider timeProvider)
    : IInventoryRepository
{
    public async Task<Reservation> ReserveAsync(IReadOnlyList<StockRequest> items, decimal maxUnitPrice, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var reserved = new List<ReservedItem>();
        // Check and decrement in one statement; use a consistent lock order.
        foreach (var item in items.OrderBy(x => x.SellingOptionId))
        {
            var affected = await db.SellingOptions
                .Where(x => x.Id == item.SellingOptionId && x.StockQuantity >= item.Quantity)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.StockQuantity, x => x.StockQuantity - item.Quantity), cancellationToken);
            if (affected == 0)
            {
                var exists = await db.SellingOptions.AnyAsync(x => x.Id == item.SellingOptionId, cancellationToken);
                throw new InventoryException(exists ? "insufficient_stock" : "selling_option_not_found",
                    exists ? $"Insufficient stock for {item.SellingOptionId}." : $"Selling option {item.SellingOptionId} does not exist.");
            }
            var price = await db.SellingOptions.Where(x => x.Id == item.SellingOptionId)
                .Select(x => x.Price).SingleAsync(cancellationToken);
            if (price > maxUnitPrice)
                throw new InventoryException("price_limit_exceeded", $"Price for {item.SellingOptionId} exceeds the current sales limit.");
            reserved.Add(new(item.SellingOptionId, item.Quantity, price));
        }
        var id = Guid.NewGuid();
        db.Reservations.Add(new StockReservation(id, timeProvider.GetUtcNow(),
            reserved.Select(x => new ReservedStockItem(x.SellingOptionId, x.Quantity, x.UnitPrice))));
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return new(id, reserved);
    }

    public async Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await db.Reservations.Where(x => x.Id == reservationId && !x.Released)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Released, true), cancellationToken);
        if (affected == 0)
        {
            if (!await db.Reservations.AnyAsync(x => x.Id == reservationId, cancellationToken))
                throw new InvalidOperationException($"Reservation {reservationId} does not exist.");
            return; // Another call already released this reservation.
        }
        var items = await db.Set<ReservedStockItem>().Where(x => x.ReservationId == reservationId)
            .OrderBy(x => x.SellingOptionId).ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            var restored = await db.SellingOptions.Where(x => x.Id == item.SellingOptionId)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.StockQuantity, x => x.StockQuantity + item.Quantity), cancellationToken);
            if (restored != 1) throw new InvalidOperationException("Reserved selling option is missing.");
        }
        await tx.CommitAsync(cancellationToken);
    }
}
