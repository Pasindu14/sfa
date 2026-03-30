using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToAllEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserReportingLines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserGeoAssignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Territories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SalesInvoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SalesInvoiceImportBatches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Routes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Regions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PricingStructures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Outlets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GRNs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Divisions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Areas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_UserReportingLines_IsDeleted",
                table: "UserReportingLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserGeoAssignments_IsDeleted",
                table: "UserGeoAssignments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Territories_IsDeleted",
                table: "Territories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_IsDeleted",
                table: "SalesInvoices",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceImportBatches_IsDeleted",
                table: "SalesInvoiceImportBatches",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_IsDeleted",
                table: "Routes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_IsDeleted",
                table: "Regions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted",
                table: "Products",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PricingStructures_IsDeleted",
                table: "PricingStructures",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Outlets_IsDeleted",
                table: "Outlets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_GRNs_IsDeleted",
                table: "GRNs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_IsDeleted",
                table: "Divisions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_IsDeleted",
                table: "Areas",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserReportingLines_IsDeleted",
                table: "UserReportingLines");

            migrationBuilder.DropIndex(
                name: "IX_UserGeoAssignments_IsDeleted",
                table: "UserGeoAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Territories_IsDeleted",
                table: "Territories");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_IsDeleted",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceImportBatches_IsDeleted",
                table: "SalesInvoiceImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_Routes_IsDeleted",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Regions_IsDeleted",
                table: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PricingStructures_IsDeleted",
                table: "PricingStructures");

            migrationBuilder.DropIndex(
                name: "IX_Outlets_IsDeleted",
                table: "Outlets");

            migrationBuilder.DropIndex(
                name: "IX_GRNs_IsDeleted",
                table: "GRNs");

            migrationBuilder.DropIndex(
                name: "IX_Divisions_IsDeleted",
                table: "Divisions");

            migrationBuilder.DropIndex(
                name: "IX_Areas_IsDeleted",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserReportingLines");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserGeoAssignments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Territories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SalesInvoiceImportBatches");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PricingStructures");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Outlets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GRNs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Divisions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Areas");
        }
    }
}
