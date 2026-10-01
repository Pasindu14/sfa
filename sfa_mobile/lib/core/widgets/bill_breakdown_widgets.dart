import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/utils/bill_breakdown.dart';

String _money(double v) => v.toStringAsFixed(2);

/// Small muted line under a list row's total: "Sales 6,128.00 · Disc −40.79 ·
/// Returns −2,352.31". Zero parts are left out; renders nothing when the bill
/// has neither a discount nor returns. Wraps instead of overflowing.
class BillBreakdownSubline extends StatelessWidget {
  final BillBreakdown breakdown;

  /// How amounts are written — defaults to two decimals. Pass the list's own
  /// formatter so the subline matches the headline total beside it.
  final String Function(double)? format;
  final TextAlign textAlign;

  const BillBreakdownSubline({
    super.key,
    required this.breakdown,
    this.format,
    this.textAlign = TextAlign.start,
  });

  @override
  Widget build(BuildContext context) {
    if (!breakdown.hasListSubline) return const SizedBox.shrink();
    final fmt = format ?? _money;

    const sep = TextSpan(text: '  ·  ');
    return Text.rich(
      TextSpan(
        children: [
          TextSpan(text: 'Sales ${fmt(breakdown.gross)}'),
          if (breakdown.hasDiscount) ...[
            sep,
            TextSpan(
              text: 'Disc −${fmt(breakdown.discount)}',
              style: const TextStyle(color: AppColors.success),
            ),
          ],
          if (breakdown.hasReturns) ...[
            sep,
            TextSpan(
              text: 'Returns −${fmt(breakdown.returns)}',
              style: const TextStyle(color: AppColors.error),
            ),
          ],
        ],
      ),
      textAlign: textAlign,
      style: GoogleFonts.barlow(
        fontSize: 10.sp,
        fontWeight: FontWeight.w500,
        color: AppColors.foregroundMuted,
      ),
    );
  }
}

/// Dark totals card for the bill detail screens:
///
///   Sales (gross) · Discount · Returns · Free issues (info) · Total
///
/// Sales and Total always show; Discount, Returns and Free issues only when
/// above zero. The Company/Distributor free-issue split shows when both are.
class BillTotalsCard extends StatelessWidget {
  final BillBreakdown breakdown;
  const BillTotalsCard({super.key, required this.breakdown});

  @override
  Widget build(BuildContext context) {
    final b = breakdown;
    return Container(
      padding: EdgeInsets.all(14.r),
      decoration: BoxDecoration(
        color: AppColors.darkSurface,
        borderRadius: BorderRadius.circular(12.r),
      ),
      child: Column(
        children: [
          _line('Sales (gross)', 'Rs. ${_money(b.gross)}'),
          if (b.hasDiscount) ...[
            SizedBox(height: 6.h),
            _line(
              'Discount (${b.discountPercent.toStringAsFixed(2)}%)',
              '−Rs. ${_money(b.discount)}',
              color: _lighten(AppColors.success),
            ),
          ],
          if (b.hasReturns) ...[
            SizedBox(height: 6.h),
            _line(
              'Returns',
              '−Rs. ${_money(b.returns)}',
              color: _lighten(AppColors.error),
            ),
          ],
          if (b.hasFreeIssue) ...[
            SizedBox(height: 6.h),
            _line('Free issues (info)', 'Rs. ${_money(b.freeIssue)}'),
            if (b.hasFreeIssueSplit) ...[
              SizedBox(height: 4.h),
              _subLine('  · By Company', b.freeIssueCompany),
              SizedBox(height: 2.h),
              _subLine('  · By Distributor', b.freeIssueDistributor),
            ],
          ],
          SizedBox(height: 8.h),
          Divider(color: Colors.white.withValues(alpha: 0.10), height: 1),
          SizedBox(height: 10.h),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'TOTAL',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 13.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 1.5,
                  color: Colors.white.withValues(alpha: 0.60),
                ),
              ),
              Text(
                'Rs. ${_money(b.total)}',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 22.sp,
                  fontWeight: FontWeight.w900,
                  letterSpacing: -0.3,
                  color: AppColors.amber,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  // The brand success/error colours are tuned for a white page; lift them so
  // they stay readable on the dark card.
  static Color _lighten(Color c) => Color.lerp(c, Colors.white, 0.35)!;

  Widget _line(String label, String value, {Color? color}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: GoogleFonts.barlow(
            fontSize: 12.sp,
            color:
                color?.withValues(alpha: 0.85) ??
                Colors.white.withValues(alpha: 0.45),
          ),
        ),
        Text(
          value,
          style: GoogleFonts.barlowCondensed(
            fontSize: 14.sp,
            fontWeight: FontWeight.w600,
            color: color ?? Colors.white.withValues(alpha: 0.80),
          ),
        ),
      ],
    );
  }

  Widget _subLine(String label, double amount) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: GoogleFonts.barlow(
            fontSize: 11.sp,
            color: Colors.white.withValues(alpha: 0.40),
          ),
        ),
        Text(
          'Rs. ${_money(amount)}',
          style: GoogleFonts.barlowCondensed(
            fontSize: 12.sp,
            fontWeight: FontWeight.w600,
            color: Colors.white.withValues(alpha: 0.65),
          ),
        ),
      ],
    );
  }
}
