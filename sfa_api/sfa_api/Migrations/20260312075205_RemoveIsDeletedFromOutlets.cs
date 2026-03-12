using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsDeletedFromOutlets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Outlets_IsDeleted",
                table: "Outlets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Outlets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Outlets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_IsDeleted",
                table: "Outlets",
                column: "IsDeleted");
        }
    }
}
