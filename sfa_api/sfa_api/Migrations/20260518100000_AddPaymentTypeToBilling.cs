using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations;

/// <inheritdoc />
public partial class AddPaymentTypeToBilling : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PaymentType",
            table: "Billings",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            defaultValue: "Cash");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PaymentType",
            table: "Billings");
    }
}
