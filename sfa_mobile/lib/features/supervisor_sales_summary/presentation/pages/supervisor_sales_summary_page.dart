import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/supervisor_sales_summary/domain/entities/rep_billing_summary.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/cubit/supervisor_sales_summary_cubit.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/cubit/supervisor_sales_summary_state.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/widgets/sales_report_filters.dart';

// Every Icons.* here must already be used by release 1.0.8+10 — a Shorebird
// patch cannot ship new glyphs of the tree-shaken icon font.

class SupervisorSalesSummaryPage extends StatelessWidget {
  const SupervisorSalesSummaryPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF5F4EE),
      body: Column(
        children: [
          const SalesReportAppBar(
            title: 'SALES SUMMARY',
            subtitle: 'Rep totals over a date range',
          ),
          Expanded(
            child:
                BlocBuilder<
                  SupervisorSalesSummaryCubit,
                  SupervisorSalesSummaryState
                >(
                  builder: (context, state) => switch (state) {
                    SalesSummaryLoadingReps() => const Center(
                      child: AppSpinner(),
                    ),
                    SalesSummaryRepsError(:final message) =>
                      SalesReportErrorBody(
                        message: message,
                        onRetry: () => context
                            .read<SupervisorSalesSummaryCubit>()
                            .loadReps(),
                      ),
                    SalesSummaryReady() => _ReadyBody(state: state),
                  },
                ),
          ),
        ],
      ),
    );
  }
}

// ── Ready body ────────────────────────────────────────────────────────────────

class _ReadyBody extends StatelessWidget {
  final SalesSummaryReady state;
  const _ReadyBody({required this.state});

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<SupervisorSalesSummaryCubit>();
    return ListView(
      padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 40.h),
      children: [
        StepCard(
          step: '01',
          label: 'SALES REP',
          icon: Icons.person_rounded,
          isComplete: state.selectedRep != null,
          child: SelectBox(
            icon: Icons.person_search_rounded,
            text: state.selectedRep?.userName ?? 'Select a sales rep...',
            filled: state.selectedRep != null,
            enabled: !state.isLoading,
            onTap: () async {
              final rep = await showModalBottomSheet<RepSummary>(
                context: context,
                isScrollControlled: true,
                backgroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.vertical(
                    top: Radius.circular(20.r),
                  ),
                ),
                builder: (_) => RepPickerSheet(
                  reps: state.reps,
                  selected: state.selectedRep,
                ),
              );
              if (rep != null) cubit.selectRep(rep);
            },
          ),
        ),
        const StepConnector(),
        StepCard(
          step: '02',
          label: 'DATE RANGE',
          icon: Icons.date_range_rounded,
          isComplete: true,
          child: DateRangeFilter(
            range: state.range,
            enabled: !state.isLoading,
            onChanged: cubit.selectRange,
          ),
        ),
        SizedBox(height: 24.h),
        ReportActionButton(
          enabled: state.canLoad,
          loading: state.isLoading,
          onTap: cubit.load,
          label: 'GET SUMMARY',
        ),
        if (state.error != null) ...[
          SizedBox(height: 16.h),
          ReportErrorBanner(message: state.error!, onRetry: cubit.load),
        ],
        if (state.summary != null) ...[
          SizedBox(height: 24.h),
          _Results(
            summary: state.summary!,
            repName: state.selectedRep?.userName ?? '',
          ),
        ],
      ],
    );
  }
}

// ── Results ───────────────────────────────────────────────────────────────────

class _Results extends StatelessWidget {
  final RepBillingSummary summary;
  final String repName;
  const _Results({required this.summary, required this.repName});

  @override
  Widget build(BuildContext context) {
    final s = summary;
    final range = DateTimeRange(start: s.from, end: s.to);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _HeroCard(
          summary: s,
          repName: repName,
          rangeLabel: fmtReportRange(range),
        ),
        SizedBox(height: 12.h),
        Row(
          children: [
            Expanded(
              child: _StatTile(
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
              child: _StatTile(
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
        _StatTile(
          icon: Icons.local_offer_rounded,
          label: 'TOTAL DISCOUNT',
          value: fmtLkr(s.totalDiscount),
          caption: 'Item-wise + bill discounts',
          color: AppColors.primary,
          wide: true,
        ),
        SizedBox(height: 10.h),
        Row(
          children: [
            Expanded(
              child: _StatTile(
                icon: Icons.replay_rounded,
                label: 'GOOD RETURN',
                value: fmtLkr(s.goodReturn),
                caption: 'Resellable stock',
                color: AppColors.primary,
              ),
            ),
            SizedBox(width: 10.w),
            Expanded(
              child: _StatTile(
                icon: Icons.warning_amber_rounded,
                label: 'MARKET RETURN',
                value: fmtLkr(s.marketReturn),
                caption: 'Damaged + expired',
                color: AppColors.primary,
              ),
            ),
          ],
        ),
        SizedBox(height: 12.h),
        _NetSalesCard(summary: s),
        SizedBox(height: 10.h),
        Text(
          'Money figures cover approved + pending bills. Rejected and '
          'cancelled bills are counted only.',
          style: GoogleFonts.barlow(
            fontSize: 10.sp,
            color: AppColors.foregroundMuted,
          ),
        ),
      ],
    );
  }
}

/// The same SALES → DISCOUNT → RETURNS → NET breakdown the rep sees on a
/// bill (cart_list.dart), totalled over the range.
class _NetSalesCard extends StatelessWidget {
  final RepBillingSummary summary;
  const _NetSalesCard({required this.summary});

  @override
  Widget build(BuildContext context) {
    final s = summary;
    Widget row(String label, String value, {bool minus = false}) => Padding(
      padding: EdgeInsets.symmetric(vertical: 5.h),
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: GoogleFonts.barlowCondensed(
                fontSize: 12.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 1.0,
                color: AppColors.foregroundMuted,
              ),
            ),
          ),
          Text(
            '${minus ? '−' : ''}$value',
            style: GoogleFonts.barlowCondensed(
              fontSize: 15.sp,
              fontWeight: FontWeight.w700,
              color: AppColors.foreground,
            ),
          ),
        ],
      ),
    );

    return Container(
      padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 14.h),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        border: Border.all(color: AppColors.primary.withValues(alpha: 0.18)),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withValues(alpha: 0.10),
            blurRadius: 14,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(
                Icons.receipt_long_rounded,
                size: 15.r,
                color: AppColors.primary,
              ),
              SizedBox(width: 6.w),
              Text(
                'NET SALES',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 12.sp,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 2.0,
                  color: AppColors.primary,
                ),
              ),
              const Spacer(),
              Text(
                'Approved + pending',
                style: GoogleFonts.barlow(
                  fontSize: 10.sp,
                  color: AppColors.foregroundMuted,
                ),
              ),
            ],
          ),
          SizedBox(height: 6.h),
          row('GROSS SALES · ITEMS SOLD', fmtLkr(s.grossSales)),
          row('DISCOUNT', fmtLkr(s.totalDiscount), minus: true),
          row('GOOD RETURN', fmtLkr(s.goodReturn), minus: true),
          row('MARKET RETURN', fmtLkr(s.marketReturn), minus: true),
          Divider(height: 14.h, color: AppColors.surfaceVariant),
          Row(
            children: [
              Expanded(
                child: Text(
                  'NET SALES',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 14.sp,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 1.2,
                    color: AppColors.foreground,
                  ),
                ),
              ),
              Text(
                fmtLkr(s.netSales),
                style: GoogleFonts.barlowCondensed(
                  fontSize: 22.sp,
                  fontWeight: FontWeight.w900,
                  letterSpacing: -0.3,
                  color: AppColors.primaryDark,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _HeroCard extends StatelessWidget {
  final RepBillingSummary summary;
  final String repName;
  final String rangeLabel;

  const _HeroCard({
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
                'NET BILLED · AFTER DISCOUNT & RETURNS',
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
                  fmtLkr(s.totalBilled),
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
class _StatTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final String caption;
  final Color color;
  final bool wide;

  const _StatTile({
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
