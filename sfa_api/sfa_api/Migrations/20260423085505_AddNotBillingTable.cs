using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotBillingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "not_billing_number_seq");

            migrationBuilder.AlterColumn<string>(
                name: "BillingItemType",
                table: "BillingItems",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Sale",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.CreateTable(
                name: "NotBillings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotBillingNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NotBillingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OutletId = table.Column<int>(type: "integer", nullable: false),
                    SalesRepId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SupervisorUserId = table.Column<int>(type: "integer", nullable: true),
                    AsmUserId = table.Column<int>(type: "integer", nullable: true),
                    RsmUserId = table.Column<int>(type: "integer", nullable: true),
                    NsmUserId = table.Column<int>(type: "integer", nullable: true),
                    RouteId = table.Column<int>(type: "integer", nullable: true),
                    DivisionId = table.Column<int>(type: "integer", nullable: true),
                    TerritoryId = table.Column<int>(type: "integer", nullable: true),
                    AreaId = table.Column<int>(type: "integer", nullable: true),
                    RegionId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotBillings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotBillings_Outlets_OutletId",
                        column: x => x.OutletId,
                        principalTable: "Outlets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotBillings_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotBillings_Users_AsmUserId",
                        column: x => x.AsmUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotBillings_Users_NsmUserId",
                        column: x => x.NsmUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotBillings_Users_RsmUserId",
                        column: x => x.RsmUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotBillings_Users_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotBillings_Users_SupervisorUserId",
                        column: x => x.SupervisorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_AreaId_NotBillingDate",
                table: "NotBillings",
                columns: new[] { "AreaId", "NotBillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_AsmUserId_NotBillingDate",
                table: "NotBillings",
                columns: new[] { "AsmUserId", "NotBillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_IsDeleted",
                table: "NotBillings",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_NotBillingNumber",
                table: "NotBillings",
                column: "NotBillingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_NsmUserId_NotBillingDate",
                table: "NotBillings",
                columns: new[] { "NsmUserId", "NotBillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_OutletId_NotBillingDate",
                table: "NotBillings",
                columns: new[] { "OutletId", "NotBillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_Reason",
                table: "NotBillings",
                column: "Reason");

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_RegionId_NotBillingDate",
                table: "NotBillings",
                columns: new[] { "RegionId", "NotBillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_RouteId",
                table: "NotBillings",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_RsmUserId_NotBillingDate",
                table: "NotBillings",
                columns: new[] { "RsmUserId", "NotBillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_SalesRepId_NotBillingDate",
                table: "NotBillings",
                columns: new[] { "SalesRepId", "NotBillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_SupervisorUserId_NotBillingDate",
                table: "NotBillings",
                columns: new[] { "SupervisorUserId", "NotBillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_TerritoryId_NotBillingDate",
                table: "NotBillings",
                columns: new[] { "TerritoryId", "NotBillingDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotBillings");

            migrationBuilder.DropSequence(
                name: "not_billing_number_seq");

            migrationBuilder.AlterColumn<string>(
                name: "BillingItemType",
                table: "BillingItems",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldDefaultValue: "Sale");
        }
    }
}
