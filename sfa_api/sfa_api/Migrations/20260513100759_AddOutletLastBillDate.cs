using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddOutletLastBillDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastBillDate",
                table: "Outlets",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill: stamp each outlet with the most recent billing date from existing records
            migrationBuilder.Sql(@"
                UPDATE ""Outlets"" SET ""LastBillDate"" = sub.""MaxDate""
                FROM (
                    SELECT ""OutletId"", MAX(""BillingDate"")::timestamptz AS ""MaxDate""
                    FROM ""Billings""
                    WHERE ""IsDeleted"" = false
                    GROUP BY ""OutletId""
                ) sub
                WHERE ""Outlets"".""Id"" = sub.""OutletId"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastBillDate",
                table: "Outlets");
        }
    }
}
