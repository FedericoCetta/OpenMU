using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddGkzMarketJewels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PendingBless",
                schema: "data",
                table: "MarketStore",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PendingChaos",
                schema: "data",
                table: "MarketStore",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PendingCreation",
                schema: "data",
                table: "MarketStore",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PendingLife",
                schema: "data",
                table: "MarketStore",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PendingSoul",
                schema: "data",
                table: "MarketStore",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingBless",
                schema: "data",
                table: "MarketStore");

            migrationBuilder.DropColumn(
                name: "PendingChaos",
                schema: "data",
                table: "MarketStore");

            migrationBuilder.DropColumn(
                name: "PendingCreation",
                schema: "data",
                table: "MarketStore");

            migrationBuilder.DropColumn(
                name: "PendingLife",
                schema: "data",
                table: "MarketStore");

            migrationBuilder.DropColumn(
                name: "PendingSoul",
                schema: "data",
                table: "MarketStore");
        }
    }
}
