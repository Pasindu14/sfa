using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class FixPurchaseOrderStatusStringValues : Migration
    {
        // Mapping: the previous migration guessed wrong old enum names.
        // Actual C# enum members: Draft(0), PendingRepApproval(1), PendingManagerApproval(2),
        // PendingDistributorFinalization(3), Finalized(4), Cancelled(5), PendingDistributorAcknowledgement(6)
        // What the previous migration wrote (wrong): Submitted(1), RepApproved(2), Approved(3),
        // Rejected(4), Acknowledged(5), Finalized(6)
        // NOTE: 'Finalized' must be corrected before 'Rejected'→'Finalized' to avoid collision.

        private static readonly string[] Tables = ["\"PurchaseOrders\"", "\"PurchaseOrderHistories\""];
        private static readonly string[] Columns = ["\"Status\"", "\"FromStatus\"", "\"ToStatus\""];

        private static string UpdateSql(string table, string column, string from, string to)
            => $"UPDATE {table} SET {column} = '{to}' WHERE {column} = '{from}';";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PurchaseOrders.Status
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "Finalized", "PendingDistributorAcknowledgement"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "Rejected", "Finalized"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "Acknowledged", "Cancelled"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "Submitted", "PendingRepApproval"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "RepApproved", "PendingManagerApproval"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "Approved", "PendingDistributorFinalization"));

            // PurchaseOrderHistories.FromStatus
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "Finalized", "PendingDistributorAcknowledgement"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "Rejected", "Finalized"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "Acknowledged", "Cancelled"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "Submitted", "PendingRepApproval"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "RepApproved", "PendingManagerApproval"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "Approved", "PendingDistributorFinalization"));

            // PurchaseOrderHistories.ToStatus
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "Finalized", "PendingDistributorAcknowledgement"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "Rejected", "Finalized"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "Acknowledged", "Cancelled"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "Submitted", "PendingRepApproval"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "RepApproved", "PendingManagerApproval"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "Approved", "PendingDistributorFinalization"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: restore the (wrong) names the previous migration wrote
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "PendingDistributorAcknowledgement", "Finalized"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "Finalized", "Rejected"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "Cancelled", "Acknowledged"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "PendingRepApproval", "Submitted"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "PendingManagerApproval", "RepApproved"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrders\"", "\"Status\"", "PendingDistributorFinalization", "Approved"));

            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "PendingDistributorAcknowledgement", "Finalized"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "Finalized", "Rejected"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "Cancelled", "Acknowledged"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "PendingRepApproval", "Submitted"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "PendingManagerApproval", "RepApproved"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"FromStatus\"", "PendingDistributorFinalization", "Approved"));

            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "PendingDistributorAcknowledgement", "Finalized"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "Finalized", "Rejected"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "Cancelled", "Acknowledged"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "PendingRepApproval", "Submitted"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "PendingManagerApproval", "RepApproved"));
            migrationBuilder.Sql(UpdateSql("\"PurchaseOrderHistories\"", "\"ToStatus\"", "PendingDistributorFinalization", "Approved"));
        }
    }
}
