using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAdjustmentsAndDistributorReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdjustmentCount",
                table: "Billings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DistributorReturnValue",
                table: "Billings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAdjustedAt",
                table: "Billings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReturnType",
                table: "BillingItems",
                type: "character varying(25)",
                maxLength: 25,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalQuantity",
                table: "BillingItems",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "BillingItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SalesRep");

            migrationBuilder.AddColumn<int>(
                name: "SourceBillingItemId",
                table: "BillingItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingId = table.Column<int>(type: "integer", nullable: false),
                    AdjustedByUserId = table.Column<int>(type: "integer", nullable: false),
                    AdjustedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OldTotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NewTotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingAdjustments_Billings_BillingId",
                        column: x => x.BillingId,
                        principalTable: "Billings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingAdjustments_Users_AdjustedByUserId",
                        column: x => x.AdjustedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingAdjustmentLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingAdjustmentId = table.Column<int>(type: "integer", nullable: false),
                    BillingItemId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    OldQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NewQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    OldTotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NewTotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReturnValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingAdjustmentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingAdjustmentLines_BillingAdjustments_BillingAdjustment~",
                        column: x => x.BillingAdjustmentId,
                        principalTable: "BillingAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_Source",
                table: "BillingItems",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_SourceBillingItemId",
                table: "BillingItems",
                column: "SourceBillingItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingAdjustmentLines_BillingAdjustmentId",
                table: "BillingAdjustmentLines",
                column: "BillingAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingAdjustments_AdjustedByUserId",
                table: "BillingAdjustments",
                column: "AdjustedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingAdjustments_BillingId_AdjustedAt",
                table: "BillingAdjustments",
                columns: new[] { "BillingId", "AdjustedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_BillingItems_BillingItems_SourceBillingItemId",
                table: "BillingItems",
                column: "SourceBillingItemId",
                principalTable: "BillingItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingItems_BillingItems_SourceBillingItemId",
                table: "BillingItems");

            migrationBuilder.DropTable(
                name: "BillingAdjustmentLines");

            migrationBuilder.DropTable(
                name: "BillingAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_BillingItems_Source",
                table: "BillingItems");

            migrationBuilder.DropIndex(
                name: "IX_BillingItems_SourceBillingItemId",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "AdjustmentCount",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "DistributorReturnValue",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "LastAdjustedAt",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "OriginalQuantity",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "SourceBillingItemId",
                table: "BillingItems");

            migrationBuilder.AlterColumn<string>(
                name: "ReturnType",
                table: "BillingItems",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(25)",
                oldMaxLength: 25,
                oldNullable: true);
        }
    }
}
