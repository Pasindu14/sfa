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
import 'package:uswatte/features/supervisor_sales_summary/presentation/widgets/sales_summary_cards.dart';

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
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SalesSummaryCards(summary: summary, repName: repName),
        SizedBox(height: 12.h),
        _NetSalesCard(summary: summary),
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

/// The same SALES → DISCOUNT → FREE ISSUE → RETURNS → NET breakdown the rep sees on a
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
          row('FREE ISSUE', fmtLkr(s.freeIssueTotal), minus: true),
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
