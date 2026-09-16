using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProximityExemptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "GpsAccuracyMeters",
                table: "Billings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProximityExemptionId",
                table: "Billings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProximityOverridden",
                table: "Billings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserProximityExemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GrantedByUserId = table.Column<int>(type: "integer", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_UserProximityExemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProximityExemptions_Users_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserProximityExemptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProximityExemptions_GrantedByUserId",
                table: "UserProximityExemptions",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProximityExemptions_IsActive",
                table: "UserProximityExemptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserProximityExemptions_IsDeleted",
                table: "UserProximityExemptions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserProximityExemptions_UserId",
                table: "UserProximityExemptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProximityExemptions_UserId_IsActive_ValidTo_Active",
                table: "UserProximityExemptions",
                columns: new[] { "UserId", "IsActive", "ValidTo" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UserProximityExemptions_ValidTo",
                table: "UserProximityExemptions",
                column: "ValidTo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProximityExemptions");

            migrationBuilder.DropColumn(
                name: "GpsAccuracyMeters",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "ProximityExemptionId",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "ProximityOverridden",
                table: "Billings");
        }
    }
}
