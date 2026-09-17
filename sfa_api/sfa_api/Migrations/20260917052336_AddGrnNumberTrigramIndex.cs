using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddGrnNumberTrigramIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GRN list search switched from Contains (strpos) to ILIKE '%term%'. The unique
            // btree on GrnNumber cannot serve a leading-wildcard match; a pg_trgm GIN index can.
            // The other searched column, SalesInvoices.VchBillNo, already has
            // IX_SalesInvoices_VchBillNo_Trgm (20260407080621_AddSalesInvoiceIndexes).
            // Raw SQL, matching 20260327075203_AddTrigramSearchIndexes — trigram indexes are not
            // part of the EF model, so this migration has no model snapshot changes.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_GRNs_GrnNumber_Trgm\" " +
                "ON \"GRNs\" USING GIN (\"GrnNumber\" gin_trgm_ops)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_GRNs_GrnNumber_Trgm\"");
        }
    }
}
