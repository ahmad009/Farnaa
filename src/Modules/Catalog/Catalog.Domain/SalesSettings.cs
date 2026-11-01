namespace Catalog.Domain;

public sealed class SalesSettings
{
    public int Id { get; private set; }
    public int MaxOrderItems { get; private set; }
    public int MaxItemQuantity { get; private set; }
    public decimal MaxUnitPrice { get; private set; }

    public void EnsureValid()
    {
        if (MaxOrderItems <= 0 || MaxItemQuantity <= 0 || MaxUnitPrice < 0 ||
            decimal.Round(MaxUnitPrice, 2) != MaxUnitPrice ||
            MaxUnitPrice > 9999999999999999.99m ||
            MaxUnitPrice > 999999999999999999.99m / MaxOrderItems / MaxItemQuantity)
            throw new InvalidOperationException("Sales settings are invalid or exceed the supported money range.");
    }
}
