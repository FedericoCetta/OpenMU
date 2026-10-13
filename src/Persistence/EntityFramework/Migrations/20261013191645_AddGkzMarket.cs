using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddGkzMarket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketStore",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerName = table.Column<string>(type: "text", nullable: false),
                    PaidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PendingZen = table.Column<long>(type: "bigint", nullable: false),
                    PendingCoins = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketStore", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketListing",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageId = table.Column<Guid>(type: "uuid", nullable: true),
                    MarketStoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketListing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketListing_ItemStorage_StorageId",
                        column: x => x.StorageId,
                        principalSchema: "data",
                        principalTable: "ItemStorage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketListing_MarketStore_MarketStoreId",
                        column: x => x.MarketStoreId,
                        principalSchema: "data",
                        principalTable: "MarketStore",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketListing_MarketStoreId",
                schema: "data",
                table: "MarketListing",
                column: "MarketStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListing_StorageId",
                schema: "data",
                table: "MarketListing",
                column: "StorageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketListing",
                schema: "data");

            migrationBuilder.DropTable(
                name: "MarketStore",
                schema: "data");
        }
    }
}
