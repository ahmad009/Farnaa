using Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ordering.Infrastructure;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ReservationId).IsUnique();
        b.HasIndex(x => new { x.CustomerId, x.CreatedAt });
        b.Property(x => x.TotalAmount).HasPrecision(20, 2);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        b.ToTable("Orders", t => t.HasCheckConstraint("CK_Orders_Total", "TotalAmount >= 0"));
        b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.HasIndex(x => new { x.OrderId, x.SellingOptionId }).IsUnique();
        // SellingOptionId is an external identifier, with no cross-database FK.
        b.ToTable("OrderItems", t =>
        {
            t.HasCheckConstraint("CK_OrderItems_Quantity", "Quantity > 0");
            t.HasCheckConstraint("CK_OrderItems_Price", "UnitPrice >= 0");
        });
    }
}
