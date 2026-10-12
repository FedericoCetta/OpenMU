using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddGkzVipAndVaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Vault2Id",
                schema: "data",
                table: "Account",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Vault3Id",
                schema: "data",
                table: "Account",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Vault4Id",
                schema: "data",
                table: "Account",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Vault5Id",
                schema: "data",
                table: "Account",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Vault6Id",
                schema: "data",
                table: "Account",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VipUntil",
                schema: "data",
                table: "Account",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Account_Vault2Id",
                schema: "data",
                table: "Account",
                column: "Vault2Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Account_Vault3Id",
                schema: "data",
                table: "Account",
                column: "Vault3Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Account_Vault4Id",
                schema: "data",
                table: "Account",
                column: "Vault4Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Account_Vault5Id",
                schema: "data",
                table: "Account",
                column: "Vault5Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Account_Vault6Id",
                schema: "data",
                table: "Account",
                column: "Vault6Id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Account_ItemStorage_Vault2Id",
                schema: "data",
                table: "Account",
                column: "Vault2Id",
                principalSchema: "data",
                principalTable: "ItemStorage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Account_ItemStorage_Vault3Id",
                schema: "data",
                table: "Account",
                column: "Vault3Id",
                principalSchema: "data",
                principalTable: "ItemStorage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Account_ItemStorage_Vault4Id",
                schema: "data",
                table: "Account",
                column: "Vault4Id",
                principalSchema: "data",
                principalTable: "ItemStorage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Account_ItemStorage_Vault5Id",
                schema: "data",
                table: "Account",
                column: "Vault5Id",
                principalSchema: "data",
                principalTable: "ItemStorage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Account_ItemStorage_Vault6Id",
                schema: "data",
                table: "Account",
                column: "Vault6Id",
                principalSchema: "data",
                principalTable: "ItemStorage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Account_ItemStorage_Vault2Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropForeignKey(
                name: "FK_Account_ItemStorage_Vault3Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropForeignKey(
                name: "FK_Account_ItemStorage_Vault4Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropForeignKey(
                name: "FK_Account_ItemStorage_Vault5Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropForeignKey(
                name: "FK_Account_ItemStorage_Vault6Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropIndex(
                name: "IX_Account_Vault2Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropIndex(
                name: "IX_Account_Vault3Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropIndex(
                name: "IX_Account_Vault4Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropIndex(
                name: "IX_Account_Vault5Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropIndex(
                name: "IX_Account_Vault6Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Vault2Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Vault3Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Vault4Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Vault5Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Vault6Id",
                schema: "data",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "VipUntil",
                schema: "data",
                table: "Account");
        }
    }
}
