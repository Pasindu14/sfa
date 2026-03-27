using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionReadinessIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Territories_UpdatedAt",
                table: "Territories");

            migrationBuilder.DropIndex(
                name: "IX_Regions_UpdatedAt",
                table: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Outlets_UpdatedAt",
                table: "Outlets");

            migrationBuilder.DropIndex(
                name: "IX_Divisions_UpdatedAt",
                table: "Divisions");

            migrationBuilder.DropIndex(
                name: "IX_Areas_UpdatedAt",
                table: "Areas");

            migrationBuilder.CreateIndex(
                name: "IX_Territories_UpdatedAt",
                table: "Territories",
                column: "UpdatedAt",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_DistributorId_ProductId_TransactedAt",
                table: "StockTransactions",
                columns: new[] { "DistributorId", "ProductId", "TransactedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Name",
                table: "Routes",
                column: "Name",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_UpdatedAt",
                table: "Regions",
                column: "UpdatedAt",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_Name",
                table: "Outlets",
                column: "Name",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_UpdatedAt",
                table: "Outlets",
                column: "UpdatedAt",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_UpdatedAt",
                table: "Divisions",
                column: "UpdatedAt",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_UpdatedAt",
                table: "Areas",
                column: "UpdatedAt",
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Territories_UpdatedAt",
                table: "Territories");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_DistributorId_ProductId_TransactedAt",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Routes_Name",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Regions_UpdatedAt",
                table: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Outlets_Name",
                table: "Outlets");

            migrationBuilder.DropIndex(
                name: "IX_Outlets_UpdatedAt",
                table: "Outlets");

            migrationBuilder.DropIndex(
                name: "IX_Divisions_UpdatedAt",
                table: "Divisions");

            migrationBuilder.DropIndex(
                name: "IX_Areas_UpdatedAt",
                table: "Areas");

            migrationBuilder.CreateIndex(
                name: "IX_Territories_UpdatedAt",
                table: "Territories",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_UpdatedAt",
                table: "Regions",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_UpdatedAt",
                table: "Outlets",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_UpdatedAt",
                table: "Divisions",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_UpdatedAt",
                table: "Areas",
                column: "UpdatedAt");
        }
    }
}
