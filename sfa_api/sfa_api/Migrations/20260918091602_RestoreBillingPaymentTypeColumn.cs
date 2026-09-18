using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class RestoreBillingPaymentTypeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Billings.PaymentType has been in the model since 2026-05-18, but the migration that
            // added it (20260518100000_AddPaymentTypeToBilling) shipped without a .Designer.cs, so
            // EF never discovered it — it was neither applied nor reported as pending. A database
            // built purely from migrations therefore lacks the column and every billing query 500s
            // ("column b.PaymentType does not exist"). Idempotent so it is a no-op on databases
            // where the column was added by hand.
            migrationBuilder.Sql("""
                ALTER TABLE "Billings"
                    ADD COLUMN IF NOT EXISTS "PaymentType" character varying(10) NOT NULL DEFAULT 'Cash';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
