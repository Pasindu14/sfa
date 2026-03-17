using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDistributorGeography : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Distributors_Divisions_DivisionId",
                table: "Distributors");

            migrationBuilder.DropForeignKey(
                name: "FK_Distributors_Routes_RouteId",
                table: "Distributors");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_DivisionId",
                table: "Distributors");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_RouteId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Distributors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "Distributors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "Distributors",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_DivisionId",
                table: "Distributors",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_RouteId",
                table: "Distributors",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Distributors_Divisions_DivisionId",
                table: "Distributors",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Distributors_Routes_RouteId",
                table: "Distributors",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
