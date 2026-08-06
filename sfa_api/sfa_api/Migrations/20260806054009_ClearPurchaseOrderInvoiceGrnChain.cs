using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class ClearPurchaseOrderInvoiceGrnChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Test data cleanup — clears the whole PurchaseOrder -> SalesInvoice -> GRN chain
            // (plus the stock it fed into) so it can be re-tested from a clean slate.
            //
            // Order matters: GRN.SalesInvoiceId, SalesInvoice.PurchaseOrderId and
            // SalesInvoice.ImportBatchId are all Restrict FKs (not Cascade), so Postgres refuses
            // a parent delete while a child row still references it. GRNItems/SalesInvoiceItems/
            // PurchaseOrderItems/PurchaseOrderHistories are Cascade children of their parent and
            // would be removed automatically, but are deleted explicitly here for clarity.
            migrationBuilder.Sql(@"DELETE FROM ""StockTransactions"";");
            migrationBuilder.Sql(@"DELETE FROM ""DistributorStocks"";");
            migrationBuilder.Sql(@"DELETE FROM ""GRNItems"";");
            migrationBuilder.Sql(@"DELETE FROM ""GRNs"";");
            migrationBuilder.Sql(@"DELETE FROM ""SalesInvoiceItems"";");
            migrationBuilder.Sql(@"DELETE FROM ""SalesInvoices"";");
            migrationBuilder.Sql(@"DELETE FROM ""PurchaseOrderItems"";");
            migrationBuilder.Sql(@"DELETE FROM ""PurchaseOrderHistories"";");
            migrationBuilder.Sql(@"DELETE FROM ""PurchaseOrders"";");
            migrationBuilder.Sql(@"DELETE FROM ""SalesInvoiceImportBatches"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted rows cannot be restored. Intentionally left as a no-op.
        }
    }
}
