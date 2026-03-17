using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddGeographyToDistributor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AreaId",
                table: "Distributors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "Distributors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "Distributors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "Distributors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TerritoryId",
                table: "Distributors",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_AreaId",
                table: "Distributors",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_DivisionId",
                table: "Distributors",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_RegionId",
                table: "Distributors",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_RouteId",
                table: "Distributors",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_TerritoryId",
                table: "Distributors",
                column: "TerritoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Distributors_Areas_AreaId",
                table: "Distributors",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Distributors_Divisions_DivisionId",
                table: "Distributors",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Distributors_Regions_RegionId",
                table: "Distributors",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Distributors_Routes_RouteId",
                table: "Distributors",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Distributors_Territories_TerritoryId",
                table: "Distributors",
                column: "TerritoryId",
                principalTable: "Territories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Distributors_Areas_AreaId",
                table: "Distributors");

            migrationBuilder.DropForeignKey(
                name: "FK_Distributors_Divisions_DivisionId",
                table: "Distributors");

            migrationBuilder.DropForeignKey(
                name: "FK_Distributors_Regions_RegionId",
                table: "Distributors");

            migrationBuilder.DropForeignKey(
                name: "FK_Distributors_Routes_RouteId",
                table: "Distributors");

            migrationBuilder.DropForeignKey(
                name: "FK_Distributors_Territories_TerritoryId",
                table: "Distributors");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_AreaId",
                table: "Distributors");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_DivisionId",
                table: "Distributors");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_RegionId",
                table: "Distributors");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_RouteId",
                table: "Distributors");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_TerritoryId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "TerritoryId",
                table: "Distributors");
        }
    }
}
