using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using sfa_api.Infrastructure.Persistence;

#nullable disable

namespace sfa_api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260421100000_UniqueDistributorTerritoryId")]
    public partial class UniqueDistributorTerritoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Distributors_TerritoryId",
                table: "Distributors");

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_TerritoryId",
                table: "Distributors",
                column: "TerritoryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Distributors_TerritoryId",
                table: "Distributors");

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_TerritoryId",
                table: "Distributors",
                column: "TerritoryId");
        }
    }
}
