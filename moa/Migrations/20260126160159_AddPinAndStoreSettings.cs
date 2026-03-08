using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moa.Migrations
{
    /// <inheritdoc />
    public partial class AddPinAndStoreSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PinHash",
                table: "RationCards",
                type: "varbinary(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PinSalt",
                table: "RationCards",
                type: "varbinary(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoreSettings",
                columns: table => new
                {
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    NearEmptyThresholdAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllowEarlyOpenNextMonth = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSettings", x => x.StoreId);
                    table.ForeignKey(
                        name: "FK_StoreSettings_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_FullName",
                table: "Customers",
                column: "FullName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreSettings");

            migrationBuilder.DropIndex(
                name: "IX_Customers_FullName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PinHash",
                table: "RationCards");

            migrationBuilder.DropColumn(
                name: "PinSalt",
                table: "RationCards");
        }
    }
}
