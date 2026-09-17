using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class RepLocationPingRepIdRecordedAtDesc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RepLocationPings_RepId_RecordedAt",
                table: "RepLocationPings");

            migrationBuilder.CreateIndex(
                name: "IX_RepLocationPings_RepId_RecordedAt",
                table: "RepLocationPings",
                columns: new[] { "RepId", "RecordedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RepLocationPings_RepId_RecordedAt",
                table: "RepLocationPings");

            migrationBuilder.CreateIndex(
                name: "IX_RepLocationPings_RepId_RecordedAt",
                table: "RepLocationPings",
                columns: new[] { "RepId", "RecordedAt" });
        }
    }
}
