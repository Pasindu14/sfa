import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/supervisor_sales_summary/domain/entities/rep_billing_summary.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/widgets/sales_report_filters.dart';

// Shared by the supervisor Sales Summary page and the sales rep home page so
// both show exactly the same cards.
//
// Every Icons.* here must already be used by release 1.0.8+10 — a Shorebird
// patch cannot ship new glyphs of the tree-shaken icon font.

/// Hero card + stat tiles for one rep's [RepBillingSummary]. No NET SALES
/// breakdown — the supervisor page adds that below.
class SalesSummaryCards extends StatelessWidget {
  final RepBillingSummary summary;
  final String repName;
  const SalesSummaryCards({
    super.key,
    required this.summary,
    required this.repName,
  });

  @override
  Widget build(BuildContext context) {
    final s = summary;
    final range = DateTimeRange(start: s.from, end: s.to);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SalesSummaryHeroCard(
          summary: s,
          repName: repName,
          rangeLabel: fmtReportRange(range),
        ),
        SizedBox(height: 12.h),
        Row(
          children: [
            Expanded(
              child: SalesSummaryStatTile(
                icon: Icons.check_circle_rounded,
                label: 'SALES · APPROVED',
                value: fmtLkr(s.approvedSales),
                caption:
                    '${s.approvedCount} bill${s.approvedCount == 1 ? '' : 's'}',
                color: AppColors.primary,
              ),
            ),
            SizedBox(width: 10.w),
            Expanded(
              child: SalesSummaryStatTile(
                icon: Icons.hourglass_top_rounded,
                label: 'PENDING APPROVAL',
                value: fmtLkr(s.pendingValue),
                caption:
                    '${s.pendingCount} bill${s.pendingCount == 1 ? '' : 's'}',
                color: AppColors.primary,
              ),
            ),
          ],
        ),
        SizedBox(height: 10.h),
        Row(
          children: [
            Expanded(
              child: SalesSummaryStatTile(
                icon: Icons.local_offer_rounded,
                label: 'DISCOUNT',
                value: fmtLkr(s.totalDiscount),
                caption: 'Item-wise + bill',
                color: AppColors.primary,
              ),
            ),
            SizedBox(width: 10.w),
            Expanded(
              child: SalesSummaryStatTile(
                icon: Icons.card_giftcard_rounded,
                label: 'FREE ISSUE',
                value: fmtLkr(s.freeIssueTotal),
                caption: 'Distributor-funded',
                color: AppColors.primary,
              ),
            ),
          ],
        ),
        SizedBox(height: 10.h),
        Row(
          children: [
            Expanded(
              child: SalesSummaryStatTile(
                icon: Icons.replay_rounded,
                label: 'GOOD RETURN',
                value: fmtLkr(s.goodReturn),
                caption: 'Resellable stock',
                color: AppColors.primary,
              ),
            ),
            SizedBox(width: 10.w),
            Expanded(
              child: SalesSummaryStatTile(
                icon: Icons.warning_amber_rounded,
                label: 'MARKET RETURN',
                value: fmtLkr(s.marketReturn),
                caption: 'Damaged + expired',
                color: AppColors.primary,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class SalesSummaryHeroCard extends StatelessWidget {
  final RepBillingSummary summary;
  final String repName;
  final String rangeLabel;

  const SalesSummaryHeroCard({
    super.key,
    required this.summary,
    required this.repName,
    required this.rangeLabel,
  });

  @override
  Widget build(BuildContext context) {
    final s = summary;
    final counts = [
      '${s.approvedCount} approved',
      '${s.pendingCount} pending',
      '${s.rejectedCount} rejected',
      '${s.cancelledCount} cancelled',
    ].join('  ·  ');

    return Container(
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [AppColors.primary, AppColors.primaryLight],
        ),
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withValues(alpha: 0.30),
            blurRadius: 20,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      padding: EdgeInsets.all(20.r),
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned(
            right: -24.w,
            top: -24.h,
            child: Container(
              width: 110.r,
              height: 110.r,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: Colors.white.withValues(alpha: 0.07),
              ),
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    padding: EdgeInsets.symmetric(
                      horizontal: 10.w,
                      vertical: 4.h,
                    ),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.18),
                      borderRadius: BorderRadius.circular(20.r),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          Icons.date_range_rounded,
                          size: 11.r,
                          color: Colors.white,
                        ),
                        SizedBox(width: 5.w),
                        Text(
                          rangeLabel.toUpperCase(),
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 10.sp,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 1.2,
                            color: Colors.white,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              SizedBox(height: 12.h),
              Text(
                repName,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: GoogleFonts.barlow(
                  fontSize: 13.sp,
                  color: Colors.white.withValues(alpha: 0.8),
                ),
              ),
              SizedBox(height: 2.h),
              Text(
                'NET BILLED · AFTER DISCOUNT, FREE ISSUE & RETURNS',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 11.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 2.0,
                  color: Colors.white.withValues(alpha: 0.75),
                ),
              ),
              FittedBox(
                fit: BoxFit.scaleDown,
                alignment: Alignment.centerLeft,
                child: Text(
                  fmtLkr(s.netSales),
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 34.sp,
                    fontWeight: FontWeight.w900,
                    height: 1.05,
                    letterSpacing: -0.5,
                    color: Colors.white,
                  ),
                ),
              ),
              SizedBox(height: 14.h),
              Container(
                padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 9.h),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.14),
                  borderRadius: BorderRadius.circular(10.r),
                ),
                child: Row(
                  children: [
                    Icon(
                      Icons.receipt_long_rounded,
                      size: 16.r,
                      color: Colors.white,
                    ),
                    SizedBox(width: 8.w),
                    Text(
                      '${s.totalBills}',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 20.sp,
                        fontWeight: FontWeight.w900,
                        height: 1.0,
                        color: Colors.white,
                      ),
                    ),
                    SizedBox(width: 6.w),
                    Text(
                      'BILLS',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 10.sp,
                        fontWeight: FontWeight.w700,
                        letterSpacing: 1.5,
                        color: Colors.white.withValues(alpha: 0.8),
                      ),
                    ),
                    SizedBox(width: 10.w),
                    Expanded(
                      child: Text(
                        counts,
                        maxLines: 2,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 10.sp,
                          fontWeight: FontWeight.w600,
                          color: Colors.white.withValues(alpha: 0.85),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Gradient tile in the rep home's quick-action style.
class SalesSummaryStatTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final String caption;
  final Color color;
  final bool wide;

  const SalesSummaryStatTile({
    super.key,
    required this.icon,
    required this.label,
    required this.value,
    required this.caption,
    required this.color,
    this.wide = false,
  });

  Color _darken(Color c) {
    final hsl = HSLColor.fromColor(c);
    return hsl.withLightness((hsl.lightness - 0.15).clamp(0.0, 1.0)).toColor();
  }

  @override
  Widget build(BuildContext context) {
    final iconBox = Container(
      width: 34.r,
      height: 34.r,
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.18),
        borderRadius: BorderRadius.circular(10.r),
      ),
      child: Icon(icon, color: Colors.white, size: 17.r),
    );
    final texts = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          label,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: GoogleFonts.barlowCondensed(
            fontSize: 10.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.2,
            color: Colors.white.withValues(alpha: 0.85),
          ),
        ),
        SizedBox(height: 2.h),
        FittedBox(
          fit: BoxFit.scaleDown,
          alignment: Alignment.centerLeft,
          child: Text(
            value,
            style: GoogleFonts.barlowCondensed(
              fontSize: 19.sp,
              fontWeight: FontWeight.w900,
              height: 1.1,
              color: Colors.white,
            ),
          ),
        ),
        Text(
          caption,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: GoogleFonts.barlow(
            fontSize: 9.sp,
            color: Colors.white.withValues(alpha: 0.75),
          ),
        ),
      ],
    );

    return Container(
      padding: EdgeInsets.all(14.r),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(16.r),
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [color, _darken(color)],
        ),
        boxShadow: [
          BoxShadow(
            color: color.withValues(alpha: 0.32),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: wide
          ? Row(
              children: [
                iconBox,
                SizedBox(width: 12.w),
                Expanded(child: texts),
              ],
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                iconBox,
                SizedBox(height: 12.h),
                texts,
              ],
            ),
    );
  }
}
