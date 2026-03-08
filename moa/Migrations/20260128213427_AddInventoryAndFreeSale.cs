using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moa.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAndFreeSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CardGroupId",
                table: "RationCards",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CardGroups",
                columns: table => new
                {
                    CardGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardGroups", x => x.CardGroupId);
                    table.ForeignKey(
                        name: "FK_CardGroups_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FreeSaleReceipts",
                columns: table => new
                {
                    FreeSaleReceiptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    SoldAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreeSaleReceipts", x => x.FreeSaleReceiptId);
                    table.ForeignKey(
                        name: "FK_FreeSaleReceipts_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreProductInventories",
                columns: table => new
                {
                    StoreProductInventoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    LastRestockQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreProductInventories", x => x.StoreProductInventoryId);
                    table.ForeignKey(
                        name: "FK_StoreProductInventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreProductInventories_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreProductMonthlyFreeSalePrices",
                columns: table => new
                {
                    StoreProductMonthlyFreeSalePriceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    YearMonth = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreProductMonthlyFreeSalePrices", x => x.StoreProductMonthlyFreeSalePriceId);
                    table.ForeignKey(
                        name: "FK_StoreProductMonthlyFreeSalePrices_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreProductMonthlyFreeSalePrices_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FreeSaleReceiptItems",
                columns: table => new
                {
                    FreeSaleReceiptItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FreeSaleReceiptId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "ROUND([Quantity] * [UnitPrice], 2)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreeSaleReceiptItems", x => x.FreeSaleReceiptItemId);
                    table.ForeignKey(
                        name: "FK_FreeSaleReceiptItems_FreeSaleReceipts_FreeSaleReceiptId",
                        column: x => x.FreeSaleReceiptId,
                        principalTable: "FreeSaleReceipts",
                        principalColumn: "FreeSaleReceiptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FreeSaleReceiptItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RationCards_CardGroupId",
                table: "RationCards",
                column: "CardGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CardGroups_StoreId",
                table: "CardGroups",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_CardGroups_StoreId_GroupName",
                table: "CardGroups",
                columns: new[] { "StoreId", "GroupName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FreeSaleReceiptItems_FreeSaleReceiptId",
                table: "FreeSaleReceiptItems",
                column: "FreeSaleReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_FreeSaleReceiptItems_FreeSaleReceiptId_ProductId",
                table: "FreeSaleReceiptItems",
                columns: new[] { "FreeSaleReceiptId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FreeSaleReceiptItems_ProductId",
                table: "FreeSaleReceiptItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FreeSaleReceipts_StoreId_SoldAt",
                table: "FreeSaleReceipts",
                columns: new[] { "StoreId", "SoldAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductInventories_ProductId",
                table: "StoreProductInventories",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductInventories_StoreId",
                table: "StoreProductInventories",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductInventories_StoreId_ProductId",
                table: "StoreProductInventories",
                columns: new[] { "StoreId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductMonthlyFreeSalePrices_ProductId",
                table: "StoreProductMonthlyFreeSalePrices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductMonthlyFreeSalePrices_StoreId_ProductId_YearMonth",
                table: "StoreProductMonthlyFreeSalePrices",
                columns: new[] { "StoreId", "ProductId", "YearMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductMonthlyFreeSalePrices_StoreId_YearMonth",
                table: "StoreProductMonthlyFreeSalePrices",
                columns: new[] { "StoreId", "YearMonth" });

            migrationBuilder.AddForeignKey(
                name: "FK_RationCards_CardGroups_CardGroupId",
                table: "RationCards",
                column: "CardGroupId",
                principalTable: "CardGroups",
                principalColumn: "CardGroupId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RationCards_CardGroups_CardGroupId",
                table: "RationCards");

            migrationBuilder.DropTable(
                name: "CardGroups");

            migrationBuilder.DropTable(
                name: "FreeSaleReceiptItems");

            migrationBuilder.DropTable(
                name: "StoreProductInventories");

            migrationBuilder.DropTable(
                name: "StoreProductMonthlyFreeSalePrices");

            migrationBuilder.DropTable(
                name: "FreeSaleReceipts");

            migrationBuilder.DropIndex(
                name: "IX_RationCards_CardGroupId",
                table: "RationCards");

            migrationBuilder.DropColumn(
                name: "CardGroupId",
                table: "RationCards");
        }
    }
}
