using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <summary>
    /// Data-only: Damage and Expire returns are now credited to the outlet, like MarketResell, so the
    /// stored bill header matches the total the mobile cart showed the rep. Re-derives ReturnValue and
    /// TotalAmount from the lines for every bill whose stored ReturnValue disagrees. DistributorReturn
    /// lines stay out — the parent line was already reduced by the same quantity.
    /// </summary>
    public partial class CreditDamageExpireReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Recompute("'MarketResell', 'Damage', 'Expire'"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Recompute("'MarketResell'"));
        }

        private static string Recompute(string creditedReturnTypes) => $"""
            UPDATE "Billings" AS b
            SET "ReturnValue" = COALESCE(r.rv, 0),
                "TotalAmount" = b."SubTotalAmount" - b."BillDiscountAmount" - COALESCE(r.rv, 0),
                "UpdatedAt"   = (now() AT TIME ZONE 'utc')
            FROM (
                SELECT bb."Id" AS "BillingId", SUM(i."TotalPrice") AS rv
                FROM "Billings" bb
                LEFT JOIN "BillingItems" i
                       ON i."BillingId" = bb."Id"
                      AND NOT i."IsDeleted"
                      AND i."BillingItemType" = 'Return'
                      AND i."ReturnType" IN ({creditedReturnTypes})
                GROUP BY bb."Id"
            ) AS r
            WHERE r."BillingId" = b."Id"
              AND b."ReturnValue" <> COALESCE(r.rv, 0);
            """;
    }
}
