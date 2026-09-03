using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesSummaryReportIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Billings_DivisionId",
                table: "Billings");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_Year_Month",
                table: "SalesTargets",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_BillingDate_DistributorStatus",
                table: "Billings",
                columns: new[] { "BillingDate", "DistributorStatus" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_DivisionId_BillingDate",
                table: "Billings",
                columns: new[] { "DivisionId", "BillingDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesTargets_Year_Month",
                table: "SalesTargets");

            migrationBuilder.DropIndex(
                name: "IX_Billings_BillingDate_DistributorStatus",
                table: "Billings");

            migrationBuilder.DropIndex(
                name: "IX_Billings_DivisionId_BillingDate",
                table: "Billings");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_DivisionId",
                table: "Billings",
                column: "DivisionId");
        }
    }
}
