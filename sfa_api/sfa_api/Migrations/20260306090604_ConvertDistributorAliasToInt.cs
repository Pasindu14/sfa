using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertDistributorAliasToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE ""Distributors""
                  ALTER COLUMN ""Alias"" TYPE integer
                  USING ""Alias""::integer;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE ""Distributors""
                  ALTER COLUMN ""Alias"" TYPE text
                  USING ""Alias""::text;");
        }
    }
}
