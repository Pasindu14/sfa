using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddOutletEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Outlets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Tel = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    ContactPerson = table.Column<string>(type: "text", nullable: true),
                    NicNo = table.Column<string>(type: "text", nullable: false),
                    VatNo = table.Column<string>(type: "text", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    OwnerDOB = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    Image = table.Column<string>(type: "text", nullable: true),
                    OutletType = table.Column<int>(type: "integer", nullable: false),
                    OutletCategory = table.Column<int>(type: "integer", nullable: false),
                    BillingPriceType = table.Column<int>(type: "integer", nullable: true),
                    ProvinceCode = table.Column<int>(type: "integer", nullable: false),
                    DistrictCode = table.Column<int>(type: "integer", nullable: false),
                    RouteId = table.Column<int>(type: "integer", nullable: false),
                    DivisionId = table.Column<int>(type: "integer", nullable: false),
                    TerritoryId = table.Column<int>(type: "integer", nullable: false),
                    AreaId = table.Column<int>(type: "integer", nullable: false),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outlets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Outlets_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_AreaId",
                table: "Outlets",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_DivisionId",
                table: "Outlets",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_IsActive",
                table: "Outlets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_IsDeleted",
                table: "Outlets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_NicNo",
                table: "Outlets",
                column: "NicNo");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_RegionId",
                table: "Outlets",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_RouteId",
                table: "Outlets",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_TerritoryId",
                table: "Outlets",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_UpdatedAt",
                table: "Outlets",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Outlets");
        }
    }
}
