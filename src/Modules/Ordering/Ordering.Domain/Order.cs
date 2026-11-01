namespace Ordering.Domain;

public enum OrderStatus { Placed = 1 }

public sealed class Order
{
    private Order() { }
    public Order(Guid reservationId, Guid customerId, DateTimeOffset createdAt, IEnumerable<OrderItem> items)
    {
        if (customerId == Guid.Empty || reservationId == Guid.Empty) throw new ArgumentException("Identifiers are required.");
        Id = Guid.NewGuid();
        ReservationId = reservationId;
        CustomerId = customerId;
        CreatedAt = createdAt;
        Status = OrderStatus.Placed;
        Items = items.ToList();
        if (Items.Count == 0) throw new ArgumentException("An order requires at least one item.");
        if (Items.Select(x => x.SellingOptionId).Distinct().Count() != Items.Count)
            throw new ArgumentException("An order cannot contain duplicate selling options.");
        TotalAmount = Items.Sum(x => checked(x.UnitPrice * x.Quantity));
    }
    public Guid Id { get; private set; }
    public Guid ReservationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public List<OrderItem> Items { get; private set; } = [];
}

public sealed class OrderItem
{
    private OrderItem() { }
    public OrderItem(Guid sellingOptionId, int quantity, decimal unitPrice)
    {
        if (sellingOptionId == Guid.Empty || quantity <= 0 || unitPrice < 0 || decimal.Round(unitPrice, 2) != unitPrice)
            throw new ArgumentException("Invalid order item.");
        Id = Guid.NewGuid();
        SellingOptionId = sellingOptionId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid SellingOptionId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
}
