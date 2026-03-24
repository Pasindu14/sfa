using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddGRNAndStockTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "grn_number_seq");

            migrationBuilder.CreateTable(
                name: "DistributorStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DistributorId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributorStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributorStocks_Distributors_DistributorId",
                        column: x => x.DistributorId,
                        principalTable: "Distributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DistributorStocks_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GRNs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GrnNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SalesInvoiceId = table.Column<int>(type: "integer", nullable: false),
                    DistributorId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedBy = table.Column<int>(type: "integer", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GRNs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GRNs_Distributors_DistributorId",
                        column: x => x.DistributorId,
                        principalTable: "Distributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GRNs_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "SalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GRNs_Users_ConfirmedBy",
                        column: x => x.ConfirmedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StockTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DistributorId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Direction = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReferenceId = table.Column<int>(type: "integer", nullable: false),
                    TransactedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransactedBy = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Distributors_DistributorId",
                        column: x => x.DistributorId,
                        principalTable: "Distributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Users_TransactedBy",
                        column: x => x.TransactedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GRNItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GrnId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GRNItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GRNItems_GRNs_GrnId",
                        column: x => x.GrnId,
                        principalTable: "GRNs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GRNItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistributorStocks_DistributorId_ProductId",
                table: "DistributorStocks",
                columns: new[] { "DistributorId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistributorStocks_ProductId",
                table: "DistributorStocks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_GRNItems_GrnId",
                table: "GRNItems",
                column: "GrnId");

            migrationBuilder.CreateIndex(
                name: "IX_GRNItems_ProductId",
                table: "GRNItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_GRNs_ConfirmedBy",
                table: "GRNs",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_GRNs_DistributorId",
                table: "GRNs",
                column: "DistributorId");

            migrationBuilder.CreateIndex(
                name: "IX_GRNs_GrnNumber",
                table: "GRNs",
                column: "GrnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GRNs_SalesInvoiceId",
                table: "GRNs",
                column: "SalesInvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_DistributorId",
                table: "StockTransactions",
                column: "DistributorId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ProductId",
                table: "StockTransactions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ReferenceType_ReferenceId",
                table: "StockTransactions",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_TransactedBy",
                table: "StockTransactions",
                column: "TransactedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DistributorStocks");

            migrationBuilder.DropTable(
                name: "GRNItems");

            migrationBuilder.DropTable(
                name: "StockTransactions");

            migrationBuilder.DropTable(
                name: "GRNs");

            migrationBuilder.DropSequence(
                name: "grn_number_seq");
        }
    }
}
