using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddStockActivityLogIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_TransactedAt_Id_Desc",
                table: "StockTransactions",
                columns: new[] { "TransactedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_TransactedBy_TransactedAt_Id_Desc",
                table: "StockTransactions",
                columns: new[] { "TransactedBy", "TransactedAt", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_TransactedAt_Id_Desc",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_TransactedBy_TransactedAt_Id_Desc",
                table: "StockTransactions");
        }
    }
}
