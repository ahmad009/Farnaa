using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Released = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesSettings",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    MaxOrderItems = table.Column<int>(type: "int", nullable: false),
                    MaxItemQuantity = table.Column<int>(type: "int", nullable: false),
                    MaxUnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesSettings", x => x.Id);
                    table.CheckConstraint("CK_SalesSettings_Limits", "MaxOrderItems > 0 AND MaxItemQuantity > 0 AND MaxUnitPrice >= 0");
                    table.CheckConstraint("CK_SalesSettings_SingleRow", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "Sellers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sellers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SellingOptions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellingOptions", x => x.Id);
                    table.CheckConstraint("CK_SellingOptions_Price", "Price >= 0");
                    table.CheckConstraint("CK_SellingOptions_Stock", "StockQuantity >= 0");
                    table.ForeignKey(
                        name: "FK_SellingOptions_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SellingOptions_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalSchema: "dbo",
                        principalTable: "Sellers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservedStockItems",
                schema: "dbo",
                columns: table => new
                {
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellingOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservedStockItems", x => new { x.ReservationId, x.SellingOptionId });
                    table.CheckConstraint("CK_ReservedStockItems_Price", "UnitPrice >= 0");
                    table.CheckConstraint("CK_ReservedStockItems_Quantity", "Quantity > 0");
                    table.ForeignKey(
                        name: "FK_ReservedStockItems_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "dbo",
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservedStockItems_SellingOptions_SellingOptionId",
                        column: x => x.SellingOptionId,
                        principalSchema: "dbo",
                        principalTable: "SellingOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Products",
                columns: new[] { "Id", "Name", "SKU" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "تیتاپ", "KB-001" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "رنگارنگ", "KB-002" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "SalesSettings",
                columns: new[] { "Id", "MaxItemQuantity", "MaxOrderItems", "MaxUnitPrice" },
                values: new object[] { 1, 100000, 100, 1000000000m });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Sellers",
                columns: new[] { "Id", "Title" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "شهرام" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "بهرام" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "SellingOptions",
                columns: new[] { "Id", "Price", "ProductId", "SellerId", "StockQuantity" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), 125.50m, new Guid("10000000-0000-0000-0000-000000000001"), new Guid("20000000-0000-0000-0000-000000000001"), 1 },
                    { new Guid("30000000-0000-0000-0000-000000000002"), 130m, new Guid("10000000-0000-0000-0000-000000000001"), new Guid("20000000-0000-0000-0000-000000000002"), 10 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                schema: "dbo",
                table: "Products",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Released_CreatedAt",
                schema: "dbo",
                table: "Reservations",
                columns: new[] { "Released", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservedStockItems_SellingOptionId",
                schema: "dbo",
                table: "ReservedStockItems",
                column: "SellingOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SellingOptions_ProductId_SellerId",
                schema: "dbo",
                table: "SellingOptions",
                columns: new[] { "ProductId", "SellerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellingOptions_SellerId",
                schema: "dbo",
                table: "SellingOptions",
                column: "SellerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservedStockItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SalesSettings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Reservations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SellingOptions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Sellers",
                schema: "dbo");
        }
    }
}
