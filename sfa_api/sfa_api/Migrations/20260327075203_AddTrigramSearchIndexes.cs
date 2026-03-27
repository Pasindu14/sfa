using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddTrigramSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable trigram extension (idempotent)
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm");

            // Regions
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Regions_Name_Trgm\" ON \"Regions\" USING GIN (\"Name\" gin_trgm_ops)");

            // Areas
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Areas_Name_Trgm\" ON \"Areas\" USING GIN (\"Name\" gin_trgm_ops)");

            // Territories
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Territories_Name_Trgm\" ON \"Territories\" USING GIN (\"Name\" gin_trgm_ops)");

            // Divisions
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Divisions_Name_Trgm\" ON \"Divisions\" USING GIN (\"Name\" gin_trgm_ops)");

            // Routes
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Routes_Name_Trgm\" ON \"Routes\" USING GIN (\"Name\" gin_trgm_ops)");

            // Outlets
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Outlets_Name_Trgm\" ON \"Outlets\" USING GIN (\"Name\" gin_trgm_ops)");

            // PricingStructures
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_PricingStructures_Name_Trgm\" ON \"PricingStructures\" USING GIN (\"Name\" gin_trgm_ops)");

            // Users
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Users_Name_Trgm\" ON \"Users\" USING GIN (\"Name\" gin_trgm_ops)");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Users_Username_Trgm\" ON \"Users\" USING GIN (\"Username\" gin_trgm_ops)");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Users_Email_Trgm\" ON \"Users\" USING GIN (\"Email\" gin_trgm_ops)");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Users_Phone_Trgm\" ON \"Users\" USING GIN (\"Phone\" gin_trgm_ops)");

            // Distributors
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Distributors_Name_Trgm\" ON \"Distributors\" USING GIN (\"Name\" gin_trgm_ops)");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Distributors_Email_Trgm\" ON \"Distributors\" USING GIN (\"Email\" gin_trgm_ops)");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Distributors_Phone_Trgm\" ON \"Distributors\" USING GIN (\"Phone\" gin_trgm_ops)");

            // Products
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Products_Code_Trgm\" ON \"Products\" USING GIN (\"Code\" gin_trgm_ops)");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Products_ItemDescription_Trgm\" ON \"Products\" USING GIN (\"ItemDescription\" gin_trgm_ops)");

            // PurchaseOrders
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_PurchaseOrders_OrderNumber_Trgm\" ON \"PurchaseOrders\" USING GIN (\"OrderNumber\" gin_trgm_ops)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Regions_Name_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Areas_Name_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Territories_Name_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Divisions_Name_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Routes_Name_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Outlets_Name_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_PricingStructures_Name_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Users_Name_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Users_Username_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Users_Email_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Users_Phone_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Distributors_Name_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Distributors_Email_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Distributors_Phone_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Products_Code_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Products_ItemDescription_Trgm\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_PurchaseOrders_OrderNumber_Trgm\"");
        }
    }
}
