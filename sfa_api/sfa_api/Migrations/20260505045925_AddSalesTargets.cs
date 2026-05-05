using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "sales_target_batch_number_seq");

            migrationBuilder.CreateTable(
                name: "SalesTargetImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    InsertedRows = table.Column<int>(type: "integer", nullable: false),
                    UpdatedRows = table.Column<int>(type: "integer", nullable: false),
                    SkippedRows = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorSummary = table.Column<string>(type: "text", nullable: true),
                    ImportedBy = table.Column<int>(type: "integer", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesTargetImportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesTargetImportBatches_Users_ImportedBy",
                        column: x => x.ImportedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportBatchId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    SalesRepId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    TargetQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SupervisorUserId = table.Column<int>(type: "integer", nullable: true),
                    AsmUserId = table.Column<int>(type: "integer", nullable: true),
                    RsmUserId = table.Column<int>(type: "integer", nullable: true),
                    NsmUserId = table.Column<int>(type: "integer", nullable: true),
                    DistributorId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_SalesTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesTargets_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesTargets_SalesTargetImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "SalesTargetImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesTargets_Users_AsmUserId",
                        column: x => x.AsmUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesTargets_Users_NsmUserId",
                        column: x => x.NsmUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesTargets_Users_RsmUserId",
                        column: x => x.RsmUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesTargets_Users_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesTargets_Users_SupervisorUserId",
                        column: x => x.SupervisorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargetImportBatches_BatchNumber",
                table: "SalesTargetImportBatches",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargetImportBatches_ImportedAt",
                table: "SalesTargetImportBatches",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargetImportBatches_ImportedBy",
                table: "SalesTargetImportBatches",
                column: "ImportedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargetImportBatches_IsDeleted",
                table: "SalesTargetImportBatches",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargetImportBatches_Status",
                table: "SalesTargetImportBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargetImportBatches_Year_Month",
                table: "SalesTargetImportBatches",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_AreaId_Year_Month",
                table: "SalesTargets",
                columns: new[] { "AreaId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_AsmUserId_Year_Month",
                table: "SalesTargets",
                columns: new[] { "AsmUserId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_DistributorId_Year_Month",
                table: "SalesTargets",
                columns: new[] { "DistributorId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_DivisionId_Year_Month",
                table: "SalesTargets",
                columns: new[] { "DivisionId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_ImportBatchId",
                table: "SalesTargets",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_IsDeleted",
                table: "SalesTargets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_NsmUserId_Year_Month",
                table: "SalesTargets",
                columns: new[] { "NsmUserId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_ProductId_Year_Month",
                table: "SalesTargets",
                columns: new[] { "ProductId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_RegionId_Year_Month",
                table: "SalesTargets",
                columns: new[] { "RegionId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_RsmUserId_Year_Month",
                table: "SalesTargets",
                columns: new[] { "RsmUserId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_SalesRepId_Year_Month",
                table: "SalesTargets",
                columns: new[] { "SalesRepId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_SalesRepId_Year_Month_ProductId",
                table: "SalesTargets",
                columns: new[] { "SalesRepId", "Year", "Month", "ProductId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_SupervisorUserId_Year_Month",
                table: "SalesTargets",
                columns: new[] { "SupervisorUserId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTargets_TerritoryId_Year_Month",
                table: "SalesTargets",
                columns: new[] { "TerritoryId", "Year", "Month" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesTargets");

            migrationBuilder.DropTable(
                name: "SalesTargetImportBatches");

            migrationBuilder.DropSequence(
                name: "sales_target_batch_number_seq");
        }
    }
}
