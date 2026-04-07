using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInvoiceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix #8 — composite covering index for GetListAsync combined filter
            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_DistributorId_Status_InvoiceDate",
                table: "SalesInvoices",
                columns: new[] { "DistributorId", "Status", "InvoiceDate" });

            // Fix #3 — trigram index for ILike('%search%') on VchBillNo
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_SalesInvoices_VchBillNo_Trgm\" " +
                "ON \"SalesInvoices\" USING GIN (\"VchBillNo\" gin_trgm_ops)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_DistributorId_Status_InvoiceDate",
                table: "SalesInvoices");

            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_SalesInvoices_VchBillNo_Trgm\"");
        }
    }
}
