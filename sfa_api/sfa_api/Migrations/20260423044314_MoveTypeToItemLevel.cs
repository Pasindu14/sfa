using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class MoveTypeToItemLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Billings_OriginalBillingId",
                table: "Billings");

            migrationBuilder.DropIndex(
                name: "IX_Billings_OriginalBillingId",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "BillingType",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "OriginalBillingId",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "ReturnType",
                table: "Billings");

            migrationBuilder.AddColumn<string>(
                name: "BillingItemType",
                table: "BillingItems",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Sale");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpireDate",
                table: "BillingItems",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnType",
                table: "BillingItems",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_BillingItemType",
                table: "BillingItems",
                column: "BillingItemType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillingItems_BillingItemType",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "BillingItemType",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "ExpireDate",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "ReturnType",
                table: "BillingItems");

            migrationBuilder.AddColumn<string>(
                name: "BillingType",
                table: "Billings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OriginalBillingId",
                table: "Billings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnType",
                table: "Billings",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Billings_OriginalBillingId",
                table: "Billings",
                column: "OriginalBillingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Billings_OriginalBillingId",
                table: "Billings",
                column: "OriginalBillingId",
                principalTable: "Billings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
