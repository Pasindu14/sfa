using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddGeoConsistencyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeoConsistencyRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TriggeredBy = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RowsChecked = table.Column<int>(type: "integer", nullable: false),
                    DriftCount = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoConsistencyRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeoConsistencyFlags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunId = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    Detail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoConsistencyFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeoConsistencyFlags_GeoConsistencyRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "GeoConsistencyRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeoConsistencyFlags_RunId",
                table: "GeoConsistencyFlags",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_GeoConsistencyRuns_RunAt",
                table: "GeoConsistencyRuns",
                column: "RunAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeoConsistencyFlags");

            migrationBuilder.DropTable(
                name: "GeoConsistencyRuns");
        }
    }
}
