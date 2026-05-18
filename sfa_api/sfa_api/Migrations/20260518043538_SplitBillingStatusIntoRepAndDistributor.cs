using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class SplitBillingStatusIntoRepAndDistributor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Billings",
                newName: "RepStatus");

            migrationBuilder.RenameIndex(
                name: "IX_Billings_Status",
                table: "Billings",
                newName: "IX_Billings_RepStatus");

            migrationBuilder.AddColumn<string>(
                name: "DistributorStatus",
                table: "Billings",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Billings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Billings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Billings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Billings_DistributorStatus",
                table: "Billings",
                column: "DistributorStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Billings_DistributorStatus",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "DistributorStatus",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Billings");

            migrationBuilder.RenameIndex(
                name: "IX_Billings_RepStatus",
                table: "Billings",
                newName: "IX_Billings_Status");

            migrationBuilder.RenameColumn(
                name: "RepStatus",
                table: "Billings",
                newName: "Status");
        }
    }
}
