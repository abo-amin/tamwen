using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moa.Migrations
{
    /// <inheritdoc />
    public partial class MoveSwipeInMachineToMonthlyBalanceFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSwipedInMachine",
                table: "CardMonthlyBalances",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSwipedInMachine",
                table: "CardMonthlyBalances");
        }
    }
}
