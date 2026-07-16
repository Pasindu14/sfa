using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetIdToStockTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FleetId",
                table: "StockTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FleetId",
                table: "DistributorStocks",
                type: "integer",
                nullable: true);

            // ── Backfill ──────────────────────────────────────────────────────────────
            // DistributorStocks.FleetId is denormalized current state, so seeding it from the
            // distributor's current fleet is exactly correct.
            migrationBuilder.Sql("""
                UPDATE "DistributorStocks" s
                SET "FleetId" = d."FleetId"
                FROM "Distributors" d
                WHERE s."DistributorId" = d."Id" AND d."FleetId" IS NOT NULL;
                """);

            // StockTransactions.FleetId is meant to be the fleet AT THE TIME of the movement, but
            // that was never recorded, so it is unrecoverable for pre-existing rows. Seeding from
            // the distributor's CURRENT fleet is a deliberate best-effort approximation: it is
            // accurate for every distributor that has never changed fleet (the overwhelming
            // majority) and wrong only for historical rows of ones that have. Rows whose
            // distributor has no fleet stay NULL, which reads as "unknown". From this migration
            // onward the value is stamped at write time and is authoritative.
            migrationBuilder.Sql("""
                UPDATE "StockTransactions" t
                SET "FleetId" = d."FleetId"
                FROM "Distributors" d
                WHERE t."DistributorId" = d."Id" AND d."FleetId" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_FleetId",
                table: "StockTransactions",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributorStocks_FleetId",
                table: "DistributorStocks",
                column: "FleetId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributorStocks_Fleets_FleetId",
                table: "DistributorStocks",
                column: "FleetId",
                principalTable: "Fleets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_Fleets_FleetId",
                table: "StockTransactions",
                column: "FleetId",
                principalTable: "Fleets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributorStocks_Fleets_FleetId",
                table: "DistributorStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_Fleets_FleetId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_FleetId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_DistributorStocks_FleetId",
                table: "DistributorStocks");

            migrationBuilder.DropColumn(
                name: "FleetId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "FleetId",
                table: "DistributorStocks");
        }
    }
}
