using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.SKU).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.SKU).IsUnique();
        b.HasData(
            new { Id = SeedIds.Product, Name = "تیتاپ", SKU = "KB-001" },
            new { Id = SeedIds.ProductTwo, Name = "رنگارنگ", SKU = "KB-002" });
    }
}

internal sealed class SellerConfiguration : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> b)
    {
        b.ToTable("Sellers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.HasData(new { Id = SeedIds.SellerOne, Title = "شهرام" }, new { Id = SeedIds.SellerTwo, Title = "بهرام" });
    }
}

internal sealed class SellingOptionConfiguration : IEntityTypeConfiguration<SellingOption>
{
    public void Configure(EntityTypeBuilder<SellingOption> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.ToTable("SellingOptions", t =>
        {
            t.HasCheckConstraint("CK_SellingOptions_Stock", "StockQuantity >= 0");
            t.HasCheckConstraint("CK_SellingOptions_Price", "Price >= 0");
        });
        b.HasIndex(x => new { x.ProductId, x.SellerId }).IsUnique();
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Seller>().WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
        b.HasData(
            new { Id = SeedIds.OptionOne, ProductId = SeedIds.Product, SellerId = SeedIds.SellerOne, Price = 125.50m, StockQuantity = 1 },
            new { Id = SeedIds.OptionTwo, ProductId = SeedIds.Product, SellerId = SeedIds.SellerTwo, Price = 130m, StockQuantity = 10 });
    }
}

internal sealed class SalesSettingsConfiguration : IEntityTypeConfiguration<SalesSettings>
{
    public void Configure(EntityTypeBuilder<SalesSettings> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.MaxUnitPrice).HasPrecision(18, 2);
        b.ToTable("SalesSettings", table =>
        {
            table.HasCheckConstraint("CK_SalesSettings_SingleRow", "Id = 1");
            table.HasCheckConstraint("CK_SalesSettings_Limits", "MaxOrderItems > 0 AND MaxItemQuantity > 0 AND MaxUnitPrice >= 0");
        });
        b.HasData(new { Id = 1, MaxOrderItems = 100, MaxItemQuantity = 100_000, MaxUnitPrice = 1_000_000_000m });
    }
}

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.Released, x.CreatedAt });
        b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ReservedItemConfiguration : IEntityTypeConfiguration<ReservedStockItem>
{
    public void Configure(EntityTypeBuilder<ReservedStockItem> b)
    {
        b.HasKey(x => new { x.ReservationId, x.SellingOptionId });
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.HasOne<SellingOption>().WithMany().HasForeignKey(x => x.SellingOptionId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("ReservedStockItems", t =>
        {
            t.HasCheckConstraint("CK_ReservedStockItems_Quantity", "Quantity > 0");
            t.HasCheckConstraint("CK_ReservedStockItems_Price", "UnitPrice >= 0");
        });
    }
}
