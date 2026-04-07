using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletionApprovalFields_DailyRouteAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeletionRejectionReason",
                table: "DailyRouteAssignments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionRequestReason",
                table: "DailyRouteAssignments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionRequestedAt",
                table: "DailyRouteAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletionRequestedBy",
                table: "DailyRouteAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionReviewedAt",
                table: "DailyRouteAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletionReviewedBy",
                table: "DailyRouteAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletionStatus",
                table: "DailyRouteAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DailyRouteAssignments_DeletionStatus",
                table: "DailyRouteAssignments",
                column: "DeletionStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyRouteAssignments_DeletionStatus",
                table: "DailyRouteAssignments");

            migrationBuilder.DropColumn(
                name: "DeletionRejectionReason",
                table: "DailyRouteAssignments");

            migrationBuilder.DropColumn(
                name: "DeletionRequestReason",
                table: "DailyRouteAssignments");

            migrationBuilder.DropColumn(
                name: "DeletionRequestedAt",
                table: "DailyRouteAssignments");

            migrationBuilder.DropColumn(
                name: "DeletionRequestedBy",
                table: "DailyRouteAssignments");

            migrationBuilder.DropColumn(
                name: "DeletionReviewedAt",
                table: "DailyRouteAssignments");

            migrationBuilder.DropColumn(
                name: "DeletionReviewedBy",
                table: "DailyRouteAssignments");

            migrationBuilder.DropColumn(
                name: "DeletionStatus",
                table: "DailyRouteAssignments");
        }
    }
}
