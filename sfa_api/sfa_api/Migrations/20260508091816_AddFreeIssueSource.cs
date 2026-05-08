using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddFreeIssueSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FreeIssueValueCompany",
                table: "Billings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FreeIssueValueDistributor",
                table: "Billings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FreeIssueSource",
                table: "BillingItems",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            // Backfill: every pre-existing FOC line was implicitly company-funded
            // (the system only ever deducted from StockType.FreeIssue before this change).
            migrationBuilder.Sql(@"
                UPDATE ""BillingItems""
                   SET ""FreeIssueSource"" = 'Company'
                 WHERE ""BillingItemType"" = 'FreeIssue'
                   AND ""FreeIssueSource"" IS NULL;
            ");

            // Mirror the historical FreeIssueValue total into the Company bucket so reports stay consistent.
            migrationBuilder.Sql(@"
                UPDATE ""Billings""
                   SET ""FreeIssueValueCompany"" = ""FreeIssueValue""
                 WHERE ""FreeIssueValue"" > 0
                   AND ""FreeIssueValueCompany"" = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreeIssueValueCompany",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "FreeIssueValueDistributor",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "FreeIssueSource",
                table: "BillingItems");
        }
    }
}
