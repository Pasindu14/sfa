using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTaking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockTakingPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "Open"),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakingPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTakingPeriods_Users_LockedBy",
                        column: x => x.LockedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTakingSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockTakingPeriodId = table.Column<int>(type: "integer", nullable: false),
                    DistributorId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "Draft"),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakingSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTakingSubmissions_Distributors_DistributorId",
                        column: x => x.DistributorId,
                        principalTable: "Distributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTakingSubmissions_StockTakingPeriods_StockTakingPeriod~",
                        column: x => x.StockTakingPeriodId,
                        principalTable: "StockTakingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTakingSubmissions_Users_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTakingLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockTakingSubmissionId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    StockType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "Normal"),
                    CountedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Variance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IsAdjusted = table.Column<bool>(type: "boolean", nullable: false),
                    AdjustedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    AdjustedBy = table.Column<int>(type: "integer", nullable: true),
                    AdjustedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakingLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTakingLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTakingLines_StockTakingSubmissions_StockTakingSubmissi~",
                        column: x => x.StockTakingSubmissionId,
                        principalTable: "StockTakingSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTakingLines_Users_AdjustedBy",
                        column: x => x.AdjustedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingLines_AdjustedBy",
                table: "StockTakingLines",
                column: "AdjustedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingLines_ProductId",
                table: "StockTakingLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingLines_StockTakingSubmissionId",
                table: "StockTakingLines",
                column: "StockTakingSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingLines_StockTakingSubmissionId_ProductId_StockType",
                table: "StockTakingLines",
                columns: new[] { "StockTakingSubmissionId", "ProductId", "StockType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingPeriods_IsDeleted",
                table: "StockTakingPeriods",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingPeriods_LockedBy",
                table: "StockTakingPeriods",
                column: "LockedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingPeriods_Month_Year",
                table: "StockTakingPeriods",
                columns: new[] { "Month", "Year" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingPeriods_Status",
                table: "StockTakingPeriods",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingSubmissions_DistributorId",
                table: "StockTakingSubmissions",
                column: "DistributorId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingSubmissions_IsDeleted",
                table: "StockTakingSubmissions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingSubmissions_Status",
                table: "StockTakingSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingSubmissions_StockTakingPeriodId_DistributorId",
                table: "StockTakingSubmissions",
                columns: new[] { "StockTakingPeriodId", "DistributorId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingSubmissions_SubmittedBy",
                table: "StockTakingSubmissions",
                column: "SubmittedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTakingLines");

            migrationBuilder.DropTable(
                name: "StockTakingSubmissions");

            migrationBuilder.DropTable(
                name: "StockTakingPeriods");
        }
    }
}
