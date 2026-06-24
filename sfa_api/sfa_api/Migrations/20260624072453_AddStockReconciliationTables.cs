using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReconciliationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockReconciliationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TriggeredBy = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GroupsChecked = table.Column<int>(type: "integer", nullable: false),
                    DiscrepancyCount = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReconciliationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockReconciliationFlags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunId = table.Column<int>(type: "integer", nullable: false),
                    DistributorId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    StockType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "Normal"),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Delta = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReconciliationFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockReconciliationFlags_StockReconciliationRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "StockReconciliationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockReconciliationFlags_DistributorId_ProductId",
                table: "StockReconciliationFlags",
                columns: new[] { "DistributorId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReconciliationFlags_RunId",
                table: "StockReconciliationFlags",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReconciliationRuns_RunAt",
                table: "StockReconciliationRuns",
                column: "RunAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockReconciliationFlags");

            migrationBuilder.DropTable(
                name: "StockReconciliationRuns");
        }
    }
}
