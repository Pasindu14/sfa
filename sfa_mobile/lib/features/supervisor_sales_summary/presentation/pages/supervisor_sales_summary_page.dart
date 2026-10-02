import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
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
          SalesSummaryResults(
            summary: state.summary!,
            repName: state.selectedRep?.userName ?? '',
          ),
        ],
      ],
    );
  }
}

// ── Results ───────────────────────────────────────────────────────────────────
