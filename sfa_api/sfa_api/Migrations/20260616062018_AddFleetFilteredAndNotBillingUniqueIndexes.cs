using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetFilteredAndNotBillingUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fleets_Name",
                table: "Fleets");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Territories",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Regions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Divisions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            // Data repair before the partial UNIQUE index (finding #3): production may already hold
            // duplicate non-deleted not-billings for the same (SalesRepId, OutletId, NotBillingDate).
            // Keep the newest (highest Id) per group and soft-delete the older duplicates so the
            // CREATE UNIQUE INDEX below can succeed. Idempotent: a re-run changes nothing.
            migrationBuilder.Sql(@"
                UPDATE ""NotBillings"" AS n
                SET ""IsDeleted"" = true,
                    ""IsActive"" = false,
                    ""UpdatedAt"" = (now() AT TIME ZONE 'UTC')
                WHERE n.""IsDeleted"" = false
                  AND EXISTS (
                      SELECT 1 FROM ""NotBillings"" AS n2
                      WHERE n2.""SalesRepId"" = n.""SalesRepId""
                        AND n2.""OutletId"" = n.""OutletId""
                        AND n2.""NotBillingDate"" = n.""NotBillingDate""
                        AND n2.""IsDeleted"" = false
                        AND n2.""Id"" > n.""Id"");");

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_SalesRepId_OutletId_NotBillingDate",
                table: "NotBillings",
                columns: new[] { "SalesRepId", "OutletId", "NotBillingDate" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            // Same data repair before the Fleets UNIQUE index — soft-delete older duplicate
            // non-deleted fleets sharing a Name, keeping the newest. Idempotent.
            migrationBuilder.Sql(@"
                UPDATE ""Fleets"" AS f
                SET ""IsDeleted"" = true,
                    ""IsActive"" = false,
                    ""UpdatedAt"" = (now() AT TIME ZONE 'UTC')
                WHERE f.""IsDeleted"" = false
                  AND EXISTS (
                      SELECT 1 FROM ""Fleets"" AS f2
                      WHERE f2.""Name"" = f.""Name""
                        AND f2.""IsDeleted"" = false
                        AND f2.""Id"" > f.""Id"");");

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_Name",
                table: "Fleets",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotBillings_SalesRepId_OutletId_NotBillingDate",
                table: "NotBillings");

            migrationBuilder.DropIndex(
                name: "IX_Fleets_Name",
                table: "Fleets");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Territories");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Divisions");

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_Name",
                table: "Fleets",
                column: "Name",
                unique: true);
        }
    }
}
