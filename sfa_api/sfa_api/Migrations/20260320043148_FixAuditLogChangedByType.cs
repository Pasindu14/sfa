using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class FixAuditLogChangedByType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert integer Status to string enum names before altering column type
            migrationBuilder.Sql(@"
                ALTER TABLE ""PurchaseOrders"" ALTER COLUMN ""Status"" TYPE text
                USING CASE ""Status""
                    WHEN 0 THEN 'Draft'
                    WHEN 1 THEN 'Submitted'
                    WHEN 2 THEN 'RepApproved'
                    WHEN 3 THEN 'Approved'
                    WHEN 4 THEN 'Rejected'
                    WHEN 5 THEN 'Acknowledged'
                    WHEN 6 THEN 'Finalized'
                    WHEN 7 THEN 'Cancelled'
                    ELSE 'Draft'
                END;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""PurchaseOrderHistories"" ALTER COLUMN ""FromStatus"" TYPE text
                USING CASE ""FromStatus""::int
                    WHEN 0 THEN 'Draft'
                    WHEN 1 THEN 'Submitted'
                    WHEN 2 THEN 'RepApproved'
                    WHEN 3 THEN 'Approved'
                    WHEN 4 THEN 'Rejected'
                    WHEN 5 THEN 'Acknowledged'
                    WHEN 6 THEN 'Finalized'
                    WHEN 7 THEN 'Cancelled'
                    ELSE NULL
                END;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""PurchaseOrderHistories"" ALTER COLUMN ""ToStatus"" TYPE text
                USING CASE ""ToStatus""::int
                    WHEN 0 THEN 'Draft'
                    WHEN 1 THEN 'Submitted'
                    WHEN 2 THEN 'RepApproved'
                    WHEN 3 THEN 'Approved'
                    WHEN 4 THEN 'Rejected'
                    WHEN 5 THEN 'Acknowledged'
                    WHEN 6 THEN 'Finalized'
                    WHEN 7 THEN 'Cancelled'
                    ELSE NULL
                END;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PurchaseOrders",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PurchaseOrderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PurchaseOrderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "ToStatus",
                table: "PurchaseOrderHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FromStatus",
                table: "PurchaseOrderHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ChangedBy",
                table: "AuditLogs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PurchaseOrderItems");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "PurchaseOrders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "ToStatus",
                table: "PurchaseOrderHistories",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FromStatus",
                table: "PurchaseOrderHistories",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ChangedBy",
                table: "AuditLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
