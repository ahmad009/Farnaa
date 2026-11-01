using Catalog.Contracts;
using Microsoft.Extensions.Logging;
using Ordering.Contracts;
using Ordering.Domain;

namespace Ordering.Application;

public sealed class PlaceOrder(ICatalogInventory inventory, IOrderRepository orders, ISalesRulesProvider salesRules,
    TimeProvider timeProvider, ILogger<PlaceOrder> logger) : IPlaceOrder
{
    public async Task<OrderResponse> ExecuteAsync(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var rules = await salesRules.GetAsync(cancellationToken);
        var items = Validate(request, rules);

        var reservation = await inventory.ReserveAsync(
            items.Select(x => new StockRequest(x.SellingOptionId, x.Quantity)).ToArray(), cancellationToken);
        try
        {
            var order = new Order(reservation.Id, request.CustomerId, timeProvider.GetUtcNow(),
                reservation.Items.Select(x => new OrderItem(x.SellingOptionId, x.Quantity, x.UnitPrice)));
            await orders.AddAsync(order, cancellationToken);
            return ToResponse(order);
        }
        catch (Exception originalError)
        {
            // Compensation must finish even if the client disconnects.
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                var committed = await orders.FindByReservationAsync(reservation.Id, cleanup.Token);
                if (committed is not null) return ToResponse(committed);
                await inventory.ReleaseAsync(reservation.Id, cleanup.Token);
            }
            catch (Exception recoveryError)
            {
                logger.LogCritical(recoveryError,
                    "Recovery requires reconciliation for reservation {ReservationId}. Original error: {OriginalError}",
                    reservation.Id, originalError.Message);
                throw new AggregateException("Order outcome or compensation could not be confirmed.", originalError, recoveryError);
            }
            throw;
        }
    }

    private static IReadOnlyList<PlaceOrderItem> Validate(PlaceOrderRequest request, SalesRules rules)
    {
        if (request.CustomerId == Guid.Empty || request.Items is not { Count: > 0 } || request.Items.Count > rules.MaxOrderItems ||
            request.Items.Any(x => x is null || x.SellingOptionId == Guid.Empty || x.Quantity <= 0 || x.Quantity > rules.MaxItemQuantity))
            throw new OrderValidationException($"CustomerId and 1-{rules.MaxOrderItems} valid items are required. Quantity must be between 1 and {rules.MaxItemQuantity}.");

        if (request.Items.Select(x => x.SellingOptionId).Distinct().Count() != request.Items.Count)
            throw new OrderValidationException("Duplicate selling options are not allowed.");

        return request.Items;
    }

    private static OrderResponse ToResponse(Order order) => new(order.Id, order.CustomerId,
        order.Status.ToString(), order.TotalAmount, order.CreatedAt,
        order.Items.Select(x => new OrderItemResponse(x.SellingOptionId, x.Quantity, x.UnitPrice)).ToArray());
}
