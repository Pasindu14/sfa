using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class FixAreaIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: xmin is a PostgreSQL system column (transaction ID) — it cannot be added
            // via migration. EF Core maps RowVersion to it automatically; no DDL needed.

            // Sync FK to Restrict — previously changed via an untracked manual migration.
            // This is a no-op in terms of behavior (DB already has Restrict) but brings
            // the EF model snapshot in sync with the actual schema.
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Regions_RegionId",
                table: "Areas");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Regions_RegionId",
                table: "Areas",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Add composite index for the common admin list query pattern.
            migrationBuilder.CreateIndex(
                name: "IX_Areas_RegionId_IsActive_IsDeleted",
                table: "Areas",
                columns: new[] { "RegionId", "IsActive", "IsDeleted" });

            // Fix IX_Areas_UpdatedAt: the initial AddAreaEntity migration created it without
            // the partial filter predicate. Re-create it with "IsActive = true" to match
            // AppDbContext and avoid an EF model/schema divergence.
            migrationBuilder.DropIndex(
                name: "IX_Areas_UpdatedAt",
                table: "Areas");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_UpdatedAt",
                table: "Areas",
                column: "UpdatedAt",
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Areas_RegionId_IsActive_IsDeleted",
                table: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_UpdatedAt",
                table: "Areas");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_UpdatedAt",
                table: "Areas",
                column: "UpdatedAt");

            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Regions_RegionId",
                table: "Areas");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Regions_RegionId",
                table: "Areas",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
