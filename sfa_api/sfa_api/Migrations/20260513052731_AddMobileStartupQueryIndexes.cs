using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileStartupQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserGeoAssignments_UserId_IsActive",
                table: "UserGeoAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Outlets_RouteId",
                table: "Outlets");

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_UserId_IsActive_Active",
                table: "UserGeoAssignments",
                columns: new[] { "UserId", "IsActive" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PricingStructures_IsDefault_IsActive",
                table: "PricingStructures",
                columns: new[] { "IsDefault", "IsActive" },
                filter: "\"IsActive\" = true AND \"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_RouteId_Active",
                table: "Outlets",
                column: "RouteId",
                filter: "\"IsActive\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DistributorStocks_DistributorId",
                table: "DistributorStocks",
                column: "DistributorId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_TerritoryId_IsActive_Active",
                table: "Distributors",
                columns: new[] { "TerritoryId", "IsActive" },
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserGeoAssignments_UserId_IsActive_Active",
                table: "UserGeoAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PricingStructures_IsDefault_IsActive",
                table: "PricingStructures");

            migrationBuilder.DropIndex(
                name: "IX_Outlets_RouteId_Active",
                table: "Outlets");

            migrationBuilder.DropIndex(
                name: "IX_DistributorStocks_DistributorId",
                table: "DistributorStocks");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_TerritoryId_IsActive_Active",
                table: "Distributors");

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_UserId_IsActive",
                table: "UserGeoAssignments",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_RouteId",
                table: "Outlets",
                column: "RouteId");
        }
    }
}
