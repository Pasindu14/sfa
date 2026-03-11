using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class routes_isactive_replace_isdeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Routes",
                newName: "IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_Routes_IsDeleted",
                table: "Routes",
                newName: "IX_Routes_IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Routes",
                newName: "IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_Routes_IsActive",
                table: "Routes",
                newName: "IX_Routes_IsDeleted");
        }
    }
}
