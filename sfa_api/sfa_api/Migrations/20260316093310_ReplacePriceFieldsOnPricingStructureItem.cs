using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePriceFieldsOnPricingStructureItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "PricingStructureItems");

            migrationBuilder.RenameColumn(
                name: "PackPrice",
                table: "PricingStructureItems",
                newName: "PromotionalPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "DealerCasePrice",
                table: "PricingStructureItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DealerPackPrice",
                table: "PricingStructureItems",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealerCasePrice",
                table: "PricingStructureItems");

            migrationBuilder.DropColumn(
                name: "DealerPackPrice",
                table: "PricingStructureItems");

            migrationBuilder.RenameColumn(
                name: "PromotionalPrice",
                table: "PricingStructureItems",
                newName: "PackPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "PricingStructureItems",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
