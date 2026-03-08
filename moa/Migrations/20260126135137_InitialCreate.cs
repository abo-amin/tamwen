using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moa.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "Owners",
                columns: table => new
                {
                    OwnerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owners", x => x.OwnerId);
                });

            migrationBuilder.CreateTable(
                name: "RationCards",
                columns: table => new
                {
                    CardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MembersCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RationCards", x => x.CardId);
                    table.ForeignKey(
                        name: "FK_RationCards_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    StoreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    StoreName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CommercialRegNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.StoreId);
                    table.ForeignKey(
                        name: "FK_Stores_Owners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Owners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CardMonthlyBalances",
                columns: table => new
                {
                    CardMonthlyBalanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardId = table.Column<int>(type: "int", nullable: false),
                    YearMonth = table.Column<int>(type: "int", nullable: false),
                    MembersCountSnapshot = table.Column<int>(type: "int", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OpenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CloseAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardMonthlyBalances", x => x.CardMonthlyBalanceId);
                    table.ForeignKey(
                        name: "FK_CardMonthlyBalances_RationCards_CardId",
                        column: x => x.CardId,
                        principalTable: "RationCards",
                        principalColumn: "CardId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerStoreAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerStoreAssignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_CustomerStoreAssignments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerStoreAssignments_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_Products_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalReceipts",
                columns: table => new
                {
                    ReceiptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    WithdrawnAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalReceipts", x => x.ReceiptId);
                    table.ForeignKey(
                        name: "FK_WithdrawalReceipts_RationCards_CardId",
                        column: x => x.CardId,
                        principalTable: "RationCards",
                        principalColumn: "CardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WithdrawalReceipts_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreProductMonthlyPrices",
                columns: table => new
                {
                    StoreProductMonthlyPriceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    YearMonth = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreProductMonthlyPrices", x => x.StoreProductMonthlyPriceId);
                    table.ForeignKey(
                        name: "FK_StoreProductMonthlyPrices_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreProductMonthlyPrices_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalAllocations",
                columns: table => new
                {
                    AllocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    CardMonthlyBalanceId = table.Column<int>(type: "int", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalAllocations", x => x.AllocationId);
                    table.ForeignKey(
                        name: "FK_WithdrawalAllocations_CardMonthlyBalances_CardMonthlyBalanceId",
                        column: x => x.CardMonthlyBalanceId,
                        principalTable: "CardMonthlyBalances",
                        principalColumn: "CardMonthlyBalanceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WithdrawalAllocations_WithdrawalReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "WithdrawalReceipts",
                        principalColumn: "ReceiptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalReceiptItems",
                columns: table => new
                {
                    ReceiptItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false, computedColumnSql: "ROUND([Quantity] * COALESCE([UnitPrice], 0), 2)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalReceiptItems", x => x.ReceiptItemId);
                    table.ForeignKey(
                        name: "FK_WithdrawalReceiptItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WithdrawalReceiptItems_WithdrawalReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "WithdrawalReceipts",
                        principalColumn: "ReceiptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalAllocationItems",
                columns: table => new
                {
                    AllocationItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllocationId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false, computedColumnSql: "ROUND([Quantity] * [UnitPrice], 2)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalAllocationItems", x => x.AllocationItemId);
                    table.ForeignKey(
                        name: "FK_WithdrawalAllocationItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WithdrawalAllocationItems_WithdrawalAllocations_AllocationId",
                        column: x => x.AllocationId,
                        principalTable: "WithdrawalAllocations",
                        principalColumn: "AllocationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardMonthlyBalances_CardId_OpenAt",
                table: "CardMonthlyBalances",
                columns: new[] { "CardId", "OpenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CardMonthlyBalances_CardId_YearMonth",
                table: "CardMonthlyBalances",
                columns: new[] { "CardId", "YearMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerStoreAssignments_CustomerId",
                table: "CustomerStoreAssignments",
                column: "CustomerId",
                unique: true,
                filter: "[EndAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerStoreAssignments_CustomerId_StartAt",
                table: "CustomerStoreAssignments",
                columns: new[] { "CustomerId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerStoreAssignments_StoreId",
                table: "CustomerStoreAssignments",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId",
                table: "Products",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_ProductName",
                table: "Products",
                columns: new[] { "StoreId", "ProductName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RationCards_CardNumber",
                table: "RationCards",
                column: "CardNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RationCards_CustomerId",
                table: "RationCards",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductMonthlyPrices_ProductId",
                table: "StoreProductMonthlyPrices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductMonthlyPrices_StoreId_ProductId_YearMonth",
                table: "StoreProductMonthlyPrices",
                columns: new[] { "StoreId", "ProductId", "YearMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductMonthlyPrices_StoreId_YearMonth",
                table: "StoreProductMonthlyPrices",
                columns: new[] { "StoreId", "YearMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_Stores_OwnerId",
                table: "Stores",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalAllocationItems_AllocationId",
                table: "WithdrawalAllocationItems",
                column: "AllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalAllocationItems_ProductId",
                table: "WithdrawalAllocationItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalAllocations_CardMonthlyBalanceId",
                table: "WithdrawalAllocations",
                column: "CardMonthlyBalanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalAllocations_ReceiptId",
                table: "WithdrawalAllocations",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalAllocations_ReceiptId_CardMonthlyBalanceId",
                table: "WithdrawalAllocations",
                columns: new[] { "ReceiptId", "CardMonthlyBalanceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalReceiptItems_ProductId",
                table: "WithdrawalReceiptItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalReceiptItems_ReceiptId_ProductId",
                table: "WithdrawalReceiptItems",
                columns: new[] { "ReceiptId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalReceipts_CardId_WithdrawnAt",
                table: "WithdrawalReceipts",
                columns: new[] { "CardId", "WithdrawnAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalReceipts_StoreId_WithdrawnAt",
                table: "WithdrawalReceipts",
                columns: new[] { "StoreId", "WithdrawnAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerStoreAssignments");

            migrationBuilder.DropTable(
                name: "StoreProductMonthlyPrices");

            migrationBuilder.DropTable(
                name: "WithdrawalAllocationItems");

            migrationBuilder.DropTable(
                name: "WithdrawalReceiptItems");

            migrationBuilder.DropTable(
                name: "WithdrawalAllocations");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "CardMonthlyBalances");

            migrationBuilder.DropTable(
                name: "WithdrawalReceipts");

            migrationBuilder.DropTable(
                name: "RationCards");

            migrationBuilder.DropTable(
                name: "Stores");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Owners");
        }
    }
}
