namespace Catalog.Domain;

public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string SKU { get; private set; } = "";
}

public sealed class Seller
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = "";
}

public sealed class SellingOption
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid SellerId { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
}

public sealed class StockReservation
{
    private StockReservation() { }
    public StockReservation(Guid id, DateTimeOffset createdAt, IEnumerable<ReservedStockItem> items)
    {
        Id = id;
        CreatedAt = createdAt;
        Items = items.ToList();
    }
    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool Released { get; private set; }
    public List<ReservedStockItem> Items { get; private set; } = [];
}

public sealed class ReservedStockItem
{
    private ReservedStockItem() { }
    public ReservedStockItem(Guid sellingOptionId, int quantity, decimal unitPrice)
    {
        SellingOptionId = sellingOptionId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
    public Guid ReservationId { get; private set; }
    public Guid SellingOptionId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
}
