using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteIdToBillings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "Billings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Billings_RouteId_BillingDate",
                table: "Billings",
                columns: new[] { "RouteId", "BillingDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Routes_RouteId",
                table: "Billings",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Routes_RouteId",
                table: "Billings");

            migrationBuilder.DropIndex(
                name: "IX_Billings_RouteId_BillingDate",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Billings");
        }
    }
}
