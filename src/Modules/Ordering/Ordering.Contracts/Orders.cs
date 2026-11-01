namespace Ordering.Contracts;

public sealed record PlaceOrderItem(Guid SellingOptionId, int Quantity);
public sealed record PlaceOrderRequest(Guid CustomerId, IReadOnlyList<PlaceOrderItem>? Items);
public sealed record OrderItemResponse(Guid SellingOptionId, int Quantity, decimal UnitPrice);
public sealed record OrderResponse(Guid Id, Guid CustomerId, string Status, decimal TotalAmount,
    DateTimeOffset CreatedAt, IReadOnlyList<OrderItemResponse> Items);

public interface IPlaceOrder
{
    Task<OrderResponse> ExecuteAsync(PlaceOrderRequest request, CancellationToken cancellationToken);
}

public sealed class OrderValidationException(string message) : Exception(message);
