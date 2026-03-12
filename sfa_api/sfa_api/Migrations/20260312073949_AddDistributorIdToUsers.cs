using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributorIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistributorId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DistributorId",
                table: "Users",
                column: "DistributorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Distributors_DistributorId",
                table: "Users",
                column: "DistributorId",
                principalTable: "Distributors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Distributors_DistributorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DistributorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DistributorId",
                table: "Users");
        }
    }
}
