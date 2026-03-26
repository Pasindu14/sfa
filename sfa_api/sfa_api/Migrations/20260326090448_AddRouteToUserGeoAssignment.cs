using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteToUserGeoAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "UserGeoAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_RouteId",
                table: "UserGeoAssignments",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserGeoAssignments_Routes_RouteId",
                table: "UserGeoAssignments",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserGeoAssignments_Routes_RouteId",
                table: "UserGeoAssignments");

            migrationBuilder.DropIndex(
                name: "IX_UserGeoAssignments_RouteId",
                table: "UserGeoAssignments");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "UserGeoAssignments");
        }
    }
}
