using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class ClearStockBalancesAndLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Current DistributorStocks/StockTransactions rows are test data only (predate the
            // GRN case-to-piece conversion fix) — wipe both rather than backfill-correcting them.
            // No FK references either table as a principal, so order between the two doesn't
            // matter; StockTransactions first purely to clear the ledger before the balances it
            // derives from.
            migrationBuilder.Sql(@"DELETE FROM ""StockTransactions"";");
            migrationBuilder.Sql(@"DELETE FROM ""DistributorStocks"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted rows cannot be restored. Intentionally left as a no-op.
        }
    }
}
