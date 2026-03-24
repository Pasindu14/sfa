using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInvoiceImportBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "sales_invoice_import_batch_number_seq");

            migrationBuilder.CreateTable(
                name: "SalesInvoiceImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TotalInvoices = table.Column<int>(type: "integer", nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorSummary = table.Column<string>(type: "text", nullable: true),
                    ImportedBy = table.Column<int>(type: "integer", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoiceImportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceImportBatches_Users_ImportedBy",
                        column: x => x.ImportedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceImportBatches_BatchNumber",
                table: "SalesInvoiceImportBatches",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceImportBatches_ImportedAt",
                table: "SalesInvoiceImportBatches",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceImportBatches_ImportedBy",
                table: "SalesInvoiceImportBatches",
                column: "ImportedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceImportBatches_Status",
                table: "SalesInvoiceImportBatches",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesInvoiceImportBatches");

            migrationBuilder.DropSequence(
                name: "sales_invoice_import_batch_number_seq");
        }
    }
}
