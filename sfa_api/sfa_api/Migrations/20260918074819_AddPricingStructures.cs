using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingStructures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HAND-EDITED — expand/contract rollout. The model no longer maps Products.DealerPackPrice /
            // DealerCasePrice / Mrp, but the columns are deliberately KEPT here: the previous API image
            // still reads them (safe rollback), and they are dropped by a separate release-2 migration.
            // An earlier AlterColumn (20260615113058) dropped their DEFAULT, so restore it — otherwise
            // every product INSERT from this build (which no longer writes them) violates NOT NULL.
            migrationBuilder.Sql("""
                ALTER TABLE "Products" ALTER COLUMN "DealerPackPrice" SET DEFAULT 0;
                ALTER TABLE "Products" ALTER COLUMN "DealerCasePrice" SET DEFAULT 0;
                ALTER TABLE "Products" ALTER COLUMN "Mrp" SET DEFAULT 0;
                """);

            migrationBuilder.AddColumn<int>(
                name: "PricingStructureId",
                table: "Billings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ListUnitPrice",
                table: "BillingItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceBasis",
                table: "BillingItems",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingStructureId",
                table: "BillingItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PricingStructures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
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
                    DealerPackPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DealerCasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Mrp = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
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
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PricingStructureItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Billings_PricingStructureId",
                table: "Billings",
                column: "PricingStructureId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_PricingStructureId",
                table: "BillingItems",
                column: "PricingStructureId");

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
                name: "IX_PricingStructures_Name",
                table: "PricingStructures",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PricingStructures_SingleDefault",
                table: "PricingStructures",
                column: "IsDefault",
                unique: true,
                filter: "\"IsDefault\" = true AND \"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingItems_PricingStructures_PricingStructureId",
                table: "BillingItems",
                column: "PricingStructureId",
                principalTable: "PricingStructures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_PricingStructures_PricingStructureId",
                table: "Billings",
                column: "PricingStructureId",
                principalTable: "PricingStructures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // HAND-EDITED — seed the initial default structure from the current product prices so no
            // price is lost. 0 has always meant "not priced" (the app refused to bill a 0 pack price),
            // so it becomes NULL. Deleted products are skipped; inactive ones are kept so reactivating
            // a product keeps its price. Existing bills keep PricingStructureId NULL (legacy) — their
            // UnitPrice is already the true historical price.
            migrationBuilder.Sql("""
                INSERT INTO "PricingStructures"
                    ("Name", "Description", "IsDefault", "IsActive", "IsDeleted", "CreatedAt", "UpdatedAt")
                VALUES
                    ('Standard', 'Migrated from product prices', true, true, false, now(), now());

                INSERT INTO "PricingStructureItems"
                    ("PricingStructureId", "ProductId", "DealerPackPrice", "DealerCasePrice", "Mrp", "CreatedAt", "UpdatedAt")
                SELECT s."Id", p."Id",
                       NULLIF(p."DealerPackPrice", 0),
                       NULLIF(p."DealerCasePrice", 0),
                       NULLIF(p."Mrp", 0),
                       now(), now()
                FROM "Products" p
                CROSS JOIN (SELECT "Id" FROM "PricingStructures" WHERE "IsDefault" AND NOT "IsDeleted") s
                WHERE NOT p."IsDeleted"
                  AND (p."DealerPackPrice" > 0 OR p."DealerCasePrice" > 0 OR p."Mrp" > 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingItems_PricingStructures_PricingStructureId",
                table: "BillingItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Billings_PricingStructures_PricingStructureId",
                table: "Billings");

            migrationBuilder.DropTable(
                name: "PricingStructureItems");

            migrationBuilder.DropTable(
                name: "PricingStructures");

            migrationBuilder.DropIndex(
                name: "IX_Billings_PricingStructureId",
                table: "Billings");

            migrationBuilder.DropIndex(
                name: "IX_BillingItems_PricingStructureId",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "PricingStructureId",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "ListUnitPrice",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "PriceBasis",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "PricingStructureId",
                table: "BillingItems");

            // HAND-EDITED — Up never dropped the Products price columns, so there is nothing to restore.
        }
    }
}
