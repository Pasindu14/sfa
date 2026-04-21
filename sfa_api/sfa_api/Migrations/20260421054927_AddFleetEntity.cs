using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FleetId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FleetId",
                table: "Distributors",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Fleets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fleets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_FleetId",
                table: "Products",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_FleetId",
                table: "Distributors",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_IsDeleted",
                table: "Fleets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_Name",
                table: "Fleets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_UpdatedAt",
                table: "Fleets",
                column: "UpdatedAt",
                filter: "\"IsActive\" = true");

            migrationBuilder.AddForeignKey(
                name: "FK_Distributors_Fleets_FleetId",
                table: "Distributors",
                column: "FleetId",
                principalTable: "Fleets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Fleets_FleetId",
                table: "Products",
                column: "FleetId",
                principalTable: "Fleets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Distributors_Fleets_FleetId",
                table: "Distributors");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Fleets_FleetId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Fleets");

            migrationBuilder.DropIndex(
                name: "IX_Products_FleetId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_FleetId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "FleetId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FleetId",
                table: "Distributors");
        }
    }
}
