using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class DropPricingStructures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Prices used to live in the default pricing structure's items. Before
            // dropping those tables, migrate each product's price forward onto the
            // product itself (the new source of truth). Only products that have NOT
            // already been given an explicit price are backfilled, so any prices set
            // directly on the product are preserved.
            migrationBuilder.Sql(@"
                UPDATE ""Products"" p
                SET ""DealerPackPrice"" = COALESCE(pi.""DealerPackPrice"", p.""DealerPackPrice""),
                    ""DealerCasePrice"" = COALESCE(pi.""DealerCasePrice"", p.""DealerCasePrice"")
                FROM ""PricingStructureItems"" pi
                JOIN ""PricingStructures"" ps ON ps.""Id"" = pi.""PricingStructureId""
                WHERE pi.""ProductId"" = p.""Id""
                  AND ps.""IsDefault"" = true
                  AND ps.""IsActive"" = true
                  AND p.""DealerPackPrice"" = 0
                  AND p.""DealerCasePrice"" = 0;
            ");

            migrationBuilder.DropTable(
                name: "PricingStructureItems");

            migrationBuilder.DropTable(
                name: "PricingStructures");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PricingStructures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingStructures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PricingStructureItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PricingStructureId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    DealerCasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DealerPackPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PromotionalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingStructureItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingStructureItems_PricingStructures_PricingStructureId",
                        column: x => x.PricingStructureId,
                        principalTable: "PricingStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PricingStructureItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PricingStructureItems_PricingStructureId_ProductId",
                table: "PricingStructureItems",
                columns: new[] { "PricingStructureId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PricingStructureItems_ProductId",
                table: "PricingStructureItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingStructures_IsActive",
                table: "PricingStructures",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PricingStructures_IsDefault",
                table: "PricingStructures",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_PricingStructures_IsDefault_IsActive",
                table: "PricingStructures",
                columns: new[] { "IsDefault", "IsActive" },
                filter: "\"IsActive\" = true AND \"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_PricingStructures_IsDeleted",
                table: "PricingStructures",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PricingStructures_Name",
                table: "PricingStructures",
                column: "Name",
                unique: true);
        }
    }
}
