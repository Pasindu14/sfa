using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseInsensitiveUserIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_Users_Email_Lower\" ON \"Users\" (LOWER(\"Email\")) WHERE \"IsActive\" = true");
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_Users_Username_Lower\" ON \"Users\" (LOWER(\"Username\")) WHERE \"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Users_Email_Lower\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Users_Username_Lower\"");
        }
    }
}
