namespace sfa_api.Features.Reports.Services;

/// <summary>
/// Splits an inclusive [from, to] range into the calendar months it overlaps, each carrying the
/// fraction of that month's days the range covers.
/// <para>
/// Sales targets are stored per (Year, Month), so an arbitrary date range cannot read one directly.
/// A month's target contributes <c>target × Factor</c>. A fully-covered month has
/// <c>Factor == 1</c>, so a whole-month report is exact and only partial months are estimated.
/// </para>
/// <para>
/// Pure and static on purpose: this is the one part of the report with no I/O, and — because the
/// SQLite test provider cannot translate SUM over decimal — it is also the part carrying most of
/// the real test coverage.
/// </para>
/// </summary>
public static class SalesSummaryProration
{
    public readonly record struct MonthWeight(int Year, int Month, decimal Factor);

    public static IReadOnlyList<MonthWeight> MonthWeights(DateOnly from, DateOnly to)
    {
        if (to < from) return [];

        var result = new List<MonthWeight>(14);
        var cursor = new DateOnly(from.Year, from.Month, 1);
        var last   = new DateOnly(to.Year,   to.Month,   1);

        while (cursor <= last)
        {
            // DaysInMonth keeps this leap-year correct without a special case.
            var daysInMonth  = DateTime.DaysInMonth(cursor.Year, cursor.Month);
            var monthEnd     = cursor.AddMonths(1).AddDays(-1);
            var overlapStart = from > cursor   ? from : cursor;
            var overlapEnd   = to   < monthEnd ? to   : monthEnd;
            var days         = overlapEnd.DayNumber - overlapStart.DayNumber + 1;

            if (days > 0)
                result.Add(new MonthWeight(cursor.Year, cursor.Month, (decimal)days / daysInMonth));

            cursor = cursor.AddMonths(1);
        }

        return result;
    }
}
