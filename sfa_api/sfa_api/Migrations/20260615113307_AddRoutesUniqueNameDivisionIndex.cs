using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutesUniqueNameDivisionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Routes_Name",
                table: "Routes");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Name_DivisionId",
                table: "Routes",
                columns: new[] { "Name", "DivisionId" },
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Routes_Name_DivisionId",
                table: "Routes");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Name",
                table: "Routes",
                column: "Name",
                filter: "\"IsActive\" = true");
        }
    }
}
