using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingsSalesRepDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Billings_SalesRepId_BillingDate"" ON ""Billings"" (""SalesRepId"", ""BillingDate"")");

            migrationBuilder.CreateIndex(
                name: "IX_NotBillings_DivisionId",
                table: "NotBillings",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_DivisionId",
                table: "Billings",
                column: "DivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Areas_AreaId",
                table: "Billings",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Divisions_DivisionId",
                table: "Billings",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Regions_RegionId",
                table: "Billings",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Territories_TerritoryId",
                table: "Billings",
                column: "TerritoryId",
                principalTable: "Territories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_NotBillings_Areas_AreaId",
                table: "NotBillings",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_NotBillings_Divisions_DivisionId",
                table: "NotBillings",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_NotBillings_Regions_RegionId",
                table: "NotBillings",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_NotBillings_Territories_TerritoryId",
                table: "NotBillings",
                column: "TerritoryId",
                principalTable: "Territories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Outlets_Areas_AreaId",
                table: "Outlets",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Outlets_Divisions_DivisionId",
                table: "Outlets",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Outlets_Regions_RegionId",
                table: "Outlets",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Outlets_Territories_TerritoryId",
                table: "Outlets",
                column: "TerritoryId",
                principalTable: "Territories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Areas_AreaId",
                table: "Billings");

            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Divisions_DivisionId",
                table: "Billings");

            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Regions_RegionId",
                table: "Billings");

            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Territories_TerritoryId",
                table: "Billings");

            migrationBuilder.DropForeignKey(
                name: "FK_NotBillings_Areas_AreaId",
                table: "NotBillings");

            migrationBuilder.DropForeignKey(
                name: "FK_NotBillings_Divisions_DivisionId",
                table: "NotBillings");

            migrationBuilder.DropForeignKey(
                name: "FK_NotBillings_Regions_RegionId",
                table: "NotBillings");

            migrationBuilder.DropForeignKey(
                name: "FK_NotBillings_Territories_TerritoryId",
                table: "NotBillings");

            migrationBuilder.DropForeignKey(
                name: "FK_Outlets_Areas_AreaId",
                table: "Outlets");

            migrationBuilder.DropForeignKey(
                name: "FK_Outlets_Divisions_DivisionId",
                table: "Outlets");

            migrationBuilder.DropForeignKey(
                name: "FK_Outlets_Regions_RegionId",
                table: "Outlets");

            migrationBuilder.DropForeignKey(
                name: "FK_Outlets_Territories_TerritoryId",
                table: "Outlets");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Billings_SalesRepId_BillingDate""");

            migrationBuilder.DropIndex(
                name: "IX_NotBillings_DivisionId",
                table: "NotBillings");

            migrationBuilder.DropIndex(
                name: "IX_Billings_DivisionId",
                table: "Billings");
        }
    }
}
