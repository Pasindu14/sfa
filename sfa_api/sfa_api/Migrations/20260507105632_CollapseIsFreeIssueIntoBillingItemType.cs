using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class CollapseIsFreeIssueIntoBillingItemType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Promote legacy (Sale, IsFreeIssue=true) rows to the new FreeIssue type.
            //    BillingItemType is stored as varchar(10) (HasConversion<string>()).
            //    Legacy FI rows were stored with UnitPrice=0; that data shape is preserved
            //    (no historical price reconstruction). New FI lines created by the updated
            //    service will carry the real selling price.
            migrationBuilder.Sql(@"
                UPDATE ""BillingItems""
                   SET ""BillingItemType"" = 'FreeIssue'
                 WHERE ""IsFreeIssue"" = TRUE
                   AND ""BillingItemType"" = 'Sale';
            ");

            // 2. Drop the now-redundant flag.
            migrationBuilder.DropColumn(
                name: "IsFreeIssue",
                table: "BillingItems");

            // 3. Add FreeIssueValue header column (default 0; legacy FI rows have UnitPrice=0
            //    so the backfill below produces 0 — accurate for the historical data we have).
            migrationBuilder.AddColumn<decimal>(
                name: "FreeIssueValue",
                table: "Billings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            // 4. Backfill FreeIssueValue from FI line totals where available.
            migrationBuilder.Sql(@"
                UPDATE ""Billings"" b
                   SET ""FreeIssueValue"" = COALESCE(sub.total, 0)
                  FROM (
                       SELECT ""BillingId"" AS bid, SUM(""TotalPrice"") AS total
                         FROM ""BillingItems""
                        WHERE ""BillingItemType"" = 'FreeIssue'
                        GROUP BY ""BillingId""
                  ) sub
                 WHERE sub.bid = b.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreeIssueValue",
                table: "Billings");

            migrationBuilder.AddColumn<bool>(
                name: "IsFreeIssue",
                table: "BillingItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Reverse step 1: convert FreeIssue rows back to (Sale, IsFreeIssue=true)
            migrationBuilder.Sql(@"
                UPDATE ""BillingItems""
                   SET ""IsFreeIssue"" = TRUE,
                       ""BillingItemType"" = 'Sale'
                 WHERE ""BillingItemType"" = 'FreeIssue';
            ");
        }
    }
}
