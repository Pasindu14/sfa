using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotBillingClientRecordId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientRecordId",
                table: "NotBillings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_ClientRecordId",
                table: "NotBillings",
                column: "ClientRecordId",
                unique: true,
                filter: "\"ClientRecordId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotBillings_ClientRecordId",
                table: "NotBillings");

            migrationBuilder.DropColumn(
                name: "ClientRecordId",
                table: "NotBillings");
        }
    }
}
