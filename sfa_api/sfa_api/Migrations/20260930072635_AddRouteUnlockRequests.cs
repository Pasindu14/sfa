using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteUnlockRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RouteUnlockRequestId",
                table: "Billings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RouteUnlockRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RouteId = table.Column<int>(type: "integer", nullable: false),
                    DailyRouteAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestLatitude = table.Column<double>(type: "double precision", nullable: true),
                    RequestLongitude = table.Column<double>(type: "double precision", nullable: true),
                    RequestGpsAccuracyMeters = table.Column<double>(type: "double precision", nullable: true),
                    SupervisorUserId = table.Column<int>(type: "integer", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ReviewedByRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteUnlockRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteUnlockRequests_DailyRouteAssignments_DailyRouteAssignm~",
                        column: x => x.DailyRouteAssignmentId,
                        principalTable: "DailyRouteAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteUnlockRequests_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteUnlockRequests_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteUnlockRequests_Users_RevokedByUserId",
                        column: x => x.RevokedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteUnlockRequests_Users_SupervisorUserId",
                        column: x => x.SupervisorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteUnlockRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RouteUnlockRequestEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RouteUnlockRequestId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PerformedByUserId = table.Column<int>(type: "integer", nullable: false),
                    PerformedByRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteUnlockRequestEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteUnlockRequestEvents_RouteUnlockRequests_RouteUnlockReq~",
                        column: x => x.RouteUnlockRequestId,
                        principalTable: "RouteUnlockRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteUnlockRequestEvents_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_RouteUnlockRequestId",
                table: "Billings",
                column: "RouteUnlockRequestId",
                filter: "\"RouteUnlockRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RouteUnlockRequestEvents_PerformedByUserId",
                table: "RouteUnlockRequestEvents",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteUnlockRequestEvents_RouteUnlockRequestId_PerformedAt",
                table: "RouteUnlockRequestEvents",
                columns: new[] { "RouteUnlockRequestId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteUnlockRequests_DailyRouteAssignmentId",
                table: "RouteUnlockRequests",
                column: "DailyRouteAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteUnlockRequests_Effective",
                table: "RouteUnlockRequests",
                columns: new[] { "UserId", "RouteId", "Status", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteUnlockRequests_RequestedAt",
                table: "RouteUnlockRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RouteUnlockRequests_ReviewedByUserId",
                table: "RouteUnlockRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteUnlockRequests_RevokedByUserId",
                table: "RouteUnlockRequests",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteUnlockRequests_RouteId",
                table: "RouteUnlockRequests",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteUnlockRequests_Status_BusinessDate",
                table: "RouteUnlockRequests",
                columns: new[] { "Status", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteUnlockRequests_SupervisorUserId",
                table: "RouteUnlockRequests",
                column: "SupervisorUserId");

            migrationBuilder.CreateIndex(
                name: "UX_RouteUnlockRequests_UserId_BusinessDate_Open",
                table: "RouteUnlockRequests",
                columns: new[] { "UserId", "BusinessDate" },
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Approved') AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteUnlockRequestEvents");

            migrationBuilder.DropTable(
                name: "RouteUnlockRequests");

            migrationBuilder.DropIndex(
                name: "IX_Billings_RouteUnlockRequestId",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "RouteUnlockRequestId",
                table: "Billings");
        }
    }
}
