using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesTargetGeoFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_SalesTargets_Areas_AreaId",
                table: "SalesTargets",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesTargets_Divisions_DivisionId",
                table: "SalesTargets",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesTargets_Regions_RegionId",
                table: "SalesTargets",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesTargets_Territories_TerritoryId",
                table: "SalesTargets",
                column: "TerritoryId",
                principalTable: "Territories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesTargets_Areas_AreaId",
                table: "SalesTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesTargets_Divisions_DivisionId",
                table: "SalesTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesTargets_Regions_RegionId",
                table: "SalesTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesTargets_Territories_TerritoryId",
                table: "SalesTargets");
        }
    }
}
