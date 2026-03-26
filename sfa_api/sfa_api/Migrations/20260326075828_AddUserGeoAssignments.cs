using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserGeoAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserGeoAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DivisionId = table.Column<int>(type: "integer", nullable: true),
                    TerritoryId = table.Column<int>(type: "integer", nullable: true),
                    AreaId = table.Column<int>(type: "integer", nullable: true),
                    RegionId = table.Column<int>(type: "integer", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGeoAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGeoAssignments_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserGeoAssignments_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserGeoAssignments_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserGeoAssignments_Territories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "Territories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserGeoAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_AreaId",
                table: "UserGeoAssignments",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_DivisionId",
                table: "UserGeoAssignments",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_EffectiveFrom",
                table: "UserGeoAssignments",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_IsActive",
                table: "UserGeoAssignments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_RegionId",
                table: "UserGeoAssignments",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_TerritoryId",
                table: "UserGeoAssignments",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_UserId",
                table: "UserGeoAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_UserId_IsActive",
                table: "UserGeoAssignments",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserGeoAssignments");
        }
    }
}
