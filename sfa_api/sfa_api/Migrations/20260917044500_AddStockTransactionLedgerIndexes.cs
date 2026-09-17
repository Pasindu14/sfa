using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransactionLedgerIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_Dist_Product_StockType_IdDesc",
                table: "StockTransactions",
                columns: new[] { "DistributorId", "ProductId", "StockType", "Id" },
                descending: new[] { false, false, false, true })
                .Annotation("Npgsql:IndexInclude", new[] { "TransactedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_DistributorId_TransactedAt",
                table: "StockTransactions",
                columns: new[] { "DistributorId", "TransactedAt" })
                .Annotation("Npgsql:IndexInclude", new[] { "ProductId", "TransactionType", "StockType", "Direction", "Quantity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_Dist_Product_StockType_IdDesc",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_DistributorId_TransactedAt",
                table: "StockTransactions");
        }
    }
}
