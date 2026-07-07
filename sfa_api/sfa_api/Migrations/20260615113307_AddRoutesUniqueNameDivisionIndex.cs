using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutesUniqueNameDivisionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Routes_Name",
                table: "Routes");

            // Data repair before the partial UNIQUE index (finding #3): production may already hold
            // duplicate ACTIVE routes with the same (Name, DivisionId), which would make the
            // CREATE UNIQUE INDEX below fail. Keep the newest (highest Id) active row per group and
            // deactivate the older duplicates (IsActive = false, per soft-delete convention — the
            // rows remain for an admin to reactivate the correct one). Idempotent: a re-run finds
            // no group with more than one active row and changes nothing.
            migrationBuilder.Sql(@"
                UPDATE ""Routes"" AS r
                SET ""IsActive"" = false,
                    ""UpdatedAt"" = (now() AT TIME ZONE 'UTC')
                WHERE r.""IsActive"" = true
                  AND EXISTS (
                      SELECT 1 FROM ""Routes"" AS r2
                      WHERE r2.""Name"" = r.""Name""
                        AND r2.""DivisionId"" = r.""DivisionId""
                        AND r2.""IsActive"" = true
                        AND r2.""Id"" > r.""Id"");");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Name_DivisionId",
                table: "Routes",
                columns: new[] { "Name", "DivisionId" },
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Routes_Name_DivisionId",
                table: "Routes");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Name",
                table: "Routes",
                column: "Name",
                filter: "\"IsActive\" = true");
        }
    }
}
