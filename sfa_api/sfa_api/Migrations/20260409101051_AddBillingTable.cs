using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "billing_number_seq");

            migrationBuilder.CreateTable(
                name: "Billings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BillingType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ReturnType = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    OriginalBillingId = table.Column<int>(type: "integer", nullable: true),
                    BillingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OutletId = table.Column<int>(type: "integer", nullable: false),
                    SalesRepId = table.Column<int>(type: "integer", nullable: false),
                    DistributorId = table.Column<int>(type: "integer", nullable: false),
                    SupervisorUserId = table.Column<int>(type: "integer", nullable: true),
                    AsmUserId = table.Column<int>(type: "integer", nullable: true),
                    RsmUserId = table.Column<int>(type: "integer", nullable: true),
                    NsmUserId = table.Column<int>(type: "integer", nullable: true),
                    DivisionId = table.Column<int>(type: "integer", nullable: true),
                    TerritoryId = table.Column<int>(type: "integer", nullable: true),
                    AreaId = table.Column<int>(type: "integer", nullable: true),
                    RegionId = table.Column<int>(type: "integer", nullable: true),
                    SubTotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BillDiscountRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    BillDiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Billings_Billings_OriginalBillingId",
                        column: x => x.OriginalBillingId,
                        principalTable: "Billings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Billings_Distributors_DistributorId",
                        column: x => x.DistributorId,
                        principalTable: "Distributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Billings_Outlets_OutletId",
                        column: x => x.OutletId,
                        principalTable: "Outlets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Billings_Users_AsmUserId",
                        column: x => x.AsmUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Billings_Users_NsmUserId",
                        column: x => x.NsmUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Billings_Users_RsmUserId",
                        column: x => x.RsmUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Billings_Users_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Billings_Users_SupervisorUserId",
                        column: x => x.SupervisorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BillingItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsFreeIssue = table.Column<bool>(type: "boolean", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingItems_Billings_BillingId",
                        column: x => x.BillingId,
                        principalTable: "Billings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_BillingId",
                table: "BillingItems",
                column: "BillingId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_ProductId",
                table: "BillingItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_AreaId_BillingDate",
                table: "Billings",
                columns: new[] { "AreaId", "BillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_AsmUserId_BillingDate",
                table: "Billings",
                columns: new[] { "AsmUserId", "BillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_BillingNumber",
                table: "Billings",
                column: "BillingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Billings_DistributorId_BillingDate",
                table: "Billings",
                columns: new[] { "DistributorId", "BillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_IsDeleted",
                table: "Billings",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_NsmUserId_BillingDate",
                table: "Billings",
                columns: new[] { "NsmUserId", "BillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_OriginalBillingId",
                table: "Billings",
                column: "OriginalBillingId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_OutletId_BillingDate",
                table: "Billings",
                columns: new[] { "OutletId", "BillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_RegionId_BillingDate",
                table: "Billings",
                columns: new[] { "RegionId", "BillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_RsmUserId_BillingDate",
                table: "Billings",
                columns: new[] { "RsmUserId", "BillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_SalesRepId_BillingDate",
                table: "Billings",
                columns: new[] { "SalesRepId", "BillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_Status",
                table: "Billings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_SupervisorUserId_BillingDate",
                table: "Billings",
                columns: new[] { "SupervisorUserId", "BillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_TerritoryId_BillingDate",
                table: "Billings",
                columns: new[] { "TerritoryId", "BillingDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingItems");

            migrationBuilder.DropTable(
                name: "Billings");

            migrationBuilder.DropSequence(
                name: "billing_number_seq");
        }
    }
}
