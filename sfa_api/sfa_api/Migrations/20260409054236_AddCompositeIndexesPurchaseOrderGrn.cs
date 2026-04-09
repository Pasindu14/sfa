using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexesPurchaseOrderGrn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GRNs_DistributorId",
                table: "GRNs");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_DistributorId_CreatedAt",
                table: "PurchaseOrders",
                columns: new[] { "DistributorId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_GRNs_DistributorId_Status_CreatedAt",
                table: "GRNs",
                columns: new[] { "DistributorId", "Status", "CreatedAt" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_DistributorId_CreatedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_GRNs_DistributorId_Status_CreatedAt",
                table: "GRNs");

            migrationBuilder.CreateIndex(
                name: "IX_GRNs_DistributorId",
                table: "GRNs",
                column: "DistributorId");
        }
    }
}
