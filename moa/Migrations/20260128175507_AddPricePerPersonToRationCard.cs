using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moa.Migrations
{
    /// <inheritdoc />
    public partial class AddPricePerPersonToRationCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PricePerPerson",
                table: "RationCards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerPerson",
                table: "RationCards");
        }
    }
}
