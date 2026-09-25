using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sfa_api.Migrations
{
    /// <summary>
    /// Data-only: bill header amounts are now summed from the UNROUNDED line math and rounded once
    /// (BillingService.RecomputeTotals), matching the mobile cart and the client's old app to the cent.
    /// Re-derives every header field that depends on it. Down restores the per-line-rounded sums.
    /// </summary>
    public partial class RoundBillTotalsOnce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Recompute(
                net:      """i."Quantity" * i."UnitPrice" - i."Quantity" * i."UnitPrice" * i."DiscountRate" / 100""",
                discount: """i."Quantity" * i."UnitPrice" * i."DiscountRate" / 100"""));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Recompute(net: """i."TotalPrice" """, discount: """i."DiscountAmount" """));
        }

        private static string Recompute(string net, string discount) => $"""
            UPDATE "Billings" AS b
            SET "SubTotalAmount"        = ROUND(x.sub, 2),
                "BillDiscountAmount"    = ROUND(x.sub * b."BillDiscountRate" / 100, 2),
                "ItemWiseTotalDiscount" = ROUND(x.disc, 2),
                "TotalDiscount"         = ROUND(x.disc + x.sub * b."BillDiscountRate" / 100, 2),
                "ReturnValue"           = ROUND(x.ret, 2),
                "TotalAmount"           = ROUND(x.sub - x.sub * b."BillDiscountRate" / 100 - x.ret, 2),
                "UpdatedAt"             = (now() AT TIME ZONE 'utc')
            FROM (
                SELECT bb."Id" AS "BillingId",
                       COALESCE(SUM(CASE WHEN i."BillingItemType" = 'Sale' THEN {net} END), 0)      AS sub,
                       COALESCE(SUM(CASE WHEN i."BillingItemType" = 'Sale' THEN {discount} END), 0) AS disc,
                       COALESCE(SUM(CASE WHEN i."BillingItemType" = 'Return'
                                          AND i."ReturnType" IN ('MarketResell', 'Damage', 'Expire')
                                         THEN {net} END), 0)                                         AS ret
                FROM "Billings" bb
                LEFT JOIN "BillingItems" i ON i."BillingId" = bb."Id" AND NOT i."IsDeleted"
                GROUP BY bb."Id"
            ) AS x
            WHERE x."BillingId" = b."Id"
              AND (b."TotalAmount"           <> ROUND(x.sub - x.sub * b."BillDiscountRate" / 100 - x.ret, 2)
                OR b."SubTotalAmount"        <> ROUND(x.sub, 2)
                OR b."ItemWiseTotalDiscount" <> ROUND(x.disc, 2));
            """;
    }
}
