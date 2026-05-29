using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountTotalsToBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ItemWiseTotalDiscount",
                table: "Billings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDiscount",
                table: "Billings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Backfill historical bills so the new snapshot columns aren't stuck at 0.
            // ItemWiseTotalDiscount = Σ DiscountAmount over Sale lines only (BillingItemType
            // is stored as a string via HasConversion<string>()). TotalDiscount adds the
            // already-persisted bill-level discount. Honors the soft-delete flag to match the
            // runtime !IsDeleted query filter.
            migrationBuilder.Sql(@"
                UPDATE ""Billings"" b SET
                    ""ItemWiseTotalDiscount"" = COALESCE((
                        SELECT SUM(i.""DiscountAmount"") FROM ""BillingItems"" i
                        WHERE i.""BillingId"" = b.""Id"" AND i.""IsDeleted"" = false
                          AND i.""BillingItemType"" = 'Sale'), 0),
                    ""TotalDiscount"" = COALESCE((
                        SELECT SUM(i.""DiscountAmount"") FROM ""BillingItems"" i
                        WHERE i.""BillingId"" = b.""Id"" AND i.""IsDeleted"" = false
                          AND i.""BillingItemType"" = 'Sale'), 0) + b.""BillDiscountAmount"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemWiseTotalDiscount",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "TotalDiscount",
                table: "Billings");
        }
    }
}
