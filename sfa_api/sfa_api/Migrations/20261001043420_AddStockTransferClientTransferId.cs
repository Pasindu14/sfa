using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransferClientTransferId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientTransferId",
                table: "StockTransfers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ClientTransferId",
                table: "StockTransfers",
                column: "ClientTransferId",
                unique: true,
                filter: "\"ClientTransferId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_ClientTransferId",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "ClientTransferId",
                table: "StockTransfers");
        }
    }
}
