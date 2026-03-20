using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class BackfillPurchaseOrderItemsIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The AddColumn migration used defaultValue: false, so all pre-existing
            // items were set to IsActive = false. Items that were not soft-deleted
            // (IsDeleted = false) are active and must be corrected to IsActive = true.
            migrationBuilder.Sql(@"
                UPDATE ""PurchaseOrderItems""
                SET ""IsActive"" = true
                WHERE ""IsDeleted"" = false;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""PurchaseOrderItems""
                SET ""IsActive"" = false
                WHERE ""IsDeleted"" = false;
            ");
        }
    }
}
