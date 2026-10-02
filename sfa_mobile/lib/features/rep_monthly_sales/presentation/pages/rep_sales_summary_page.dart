import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:uswatte/features/rep_monthly_sales/presentation/cubit/rep_sales_summary_cubit.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/widgets/sales_report_filters.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/widgets/sales_summary_cards.dart';

// Every Icons.* here must already be used by release 1.0.8+10 — a Shorebird
// patch cannot ship new glyphs of the tree-shaken icon font.

/// The sales rep's own Sales Summary: same cards as the supervisor's page, but
/// the rep is the logged-in user, so there is no rep picker.
class RepSalesSummaryPage extends StatelessWidget {
  const RepSalesSummaryPage({super.key});

  @override
  Widget build(BuildContext context) {
    final name = context.select<AuthBloc, String>(
      (bloc) => bloc.state is AuthAuthenticated
          ? (bloc.state as AuthAuthenticated).name
          : '',
    );
    final repName = name.isNotEmpty ? name : 'Sales Rep';
    final cubit = context.read<RepSalesSummaryCubit>();

    return Scaffold(
      backgroundColor: const Color(0xFFF5F4EE),
      body: Column(
        children: [
          const SalesReportAppBar(
            title: 'SALES SUMMARY',
            subtitle: 'My totals over a date range',
          ),
          Expanded(
            child: BlocBuilder<RepSalesSummaryCubit, RepSalesSummaryState>(
              builder: (context, state) => ListView(
                padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 40.h),
                children: [
                  StepCard(
                    step: '01',
                    label: 'SALES REP',
                    icon: Icons.person_rounded,
                    isComplete: true,
                    child: SelectBox(
                      icon: Icons.person_search_rounded,
                      text: repName,
                      filled: true,
                      enabled: false,
                      showChevron: false,
                      onTap: () {},
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
                    enabled: !state.isLoading,
                    loading: state.isLoading,
                    onTap: cubit.load,
                    label: 'GET SUMMARY',
                  ),
                  if (state.error != null) ...[
                    SizedBox(height: 16.h),
                    ReportErrorBanner(
                        message: state.error!, onRetry: cubit.load),
                  ],
                  if (state.summary != null) ...[
                    SizedBox(height: 24.h),
                    SalesSummaryResults(
                      summary: state.summary!,
                      repName: repName,
                    ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
