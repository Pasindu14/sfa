using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyRouteAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyRouteAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RouteId = table.Column<int>(type: "integer", nullable: false),
                    AssignedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyRouteAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyRouteAssignments_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyRouteAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyRouteAssignments_AssignedDate",
                table: "DailyRouteAssignments",
                column: "AssignedDate");

            migrationBuilder.CreateIndex(
                name: "IX_DailyRouteAssignments_IsDeleted",
                table: "DailyRouteAssignments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DailyRouteAssignments_RouteId",
                table: "DailyRouteAssignments",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyRouteAssignments_RouteId_AssignedDate",
                table: "DailyRouteAssignments",
                columns: new[] { "RouteId", "AssignedDate" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DailyRouteAssignments_UserId",
                table: "DailyRouteAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyRouteAssignments_UserId_AssignedDate",
                table: "DailyRouteAssignments",
                columns: new[] { "UserId", "AssignedDate" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyRouteAssignments");
        }
    }
}
