using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetFilteredAndNotBillingUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fleets_Name",
                table: "Fleets");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Territories",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Regions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Divisions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_SalesRepId_OutletId_NotBillingDate",
                table: "NotBillings",
                columns: new[] { "SalesRepId", "OutletId", "NotBillingDate" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_Name",
                table: "Fleets",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotBillings_SalesRepId_OutletId_NotBillingDate",
                table: "NotBillings");

            migrationBuilder.DropIndex(
                name: "IX_Fleets_Name",
                table: "Fleets");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Territories");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Divisions");

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_Name",
                table: "Fleets",
                column: "Name",
                unique: true);
        }
    }
}
