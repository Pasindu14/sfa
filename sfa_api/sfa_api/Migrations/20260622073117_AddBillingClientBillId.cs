using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingClientBillId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientBillId",
                table: "Billings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Billings_ClientBillId",
                table: "Billings",
                column: "ClientBillId",
                unique: true,
                filter: "\"ClientBillId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Billings_ClientBillId",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "ClientBillId",
                table: "Billings");
        }
    }
}
