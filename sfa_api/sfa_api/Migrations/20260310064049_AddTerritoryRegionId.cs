using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddTerritoryRegionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "Territories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: resolve RegionId from the parent Area for all existing rows
            migrationBuilder.Sql(@"
                UPDATE ""Territories"" t
                SET ""RegionId"" = a.""RegionId""
                FROM ""Areas"" a
                WHERE t.""AreaId"" = a.""Id"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Territories_RegionId",
                table: "Territories",
                column: "RegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Territories_Regions_RegionId",
                table: "Territories",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Territories_Regions_RegionId",
                table: "Territories");

            migrationBuilder.DropIndex(
                name: "IX_Territories_RegionId",
                table: "Territories");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "Territories");
        }
    }
}
