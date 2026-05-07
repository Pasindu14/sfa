using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTypeToDistributorStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DistributorStocks_DistributorId_ProductId",
                table: "DistributorStocks");

            migrationBuilder.AddColumn<string>(
                name: "StockType",
                table: "StockTransactions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.AddColumn<bool>(
                name: "IsFreeIssue",
                table: "GRNItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StockType",
                table: "DistributorStocks",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.CreateIndex(
                name: "IX_DistributorStocks_DistributorId_ProductId_StockType",
                table: "DistributorStocks",
                columns: new[] { "DistributorId", "ProductId", "StockType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DistributorStocks_DistributorId_ProductId_StockType",
                table: "DistributorStocks");

            migrationBuilder.DropColumn(
                name: "StockType",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "IsFreeIssue",
                table: "GRNItems");

            migrationBuilder.DropColumn(
                name: "StockType",
                table: "DistributorStocks");

            migrationBuilder.CreateIndex(
                name: "IX_DistributorStocks_DistributorId_ProductId",
                table: "DistributorStocks",
                columns: new[] { "DistributorId", "ProductId" },
                unique: true);
        }
    }
}
