import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/outlet_billings/presentation/cubit/outlet_billings_cubit.dart';
import 'package:uswatte/features/outlet_billings/presentation/cubit/outlet_billings_state.dart';
import 'package:uswatte/features/outlet_billings/presentation/widgets/month_filter_chips.dart';
import 'package:uswatte/features/outlet_billings/presentation/widgets/outlet_billing_card.dart';
import 'package:uswatte/features/outlet_billings/presentation/widgets/route_list_tile.dart';

class OutletBillingsPage extends StatelessWidget {
  const OutletBillingsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => getIt<OutletBillingsCubit>()..load(),
      child: const _OutletBillingsView(),
    );
  }
}

class _OutletBillingsView extends StatelessWidget {
  const _OutletBillingsView();

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: AppColors.background,
      body: Column(
        children: [
          // ── Gradient header ────────────────────────────────────────────────
          Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [AppColors.primaryDark, AppColors.primary],
              ),
            ),
            child: SafeArea(
              bottom: false,
              child: Padding(
                padding: EdgeInsets.fromLTRB(8.w, 4.h, 8.w, 16.h),
                child: Row(
                  children: [
                    GestureDetector(
                      onTap: () {
                        if (context.canPop()) {
                          context.pop();
                        } else {
                          context.goNamed('salesRepHome');
                        }
                      },
                      child: Container(
                        width: 40.r,
                        height: 40.r,
                        margin: EdgeInsets.all(4.r),
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(10.r),
                          border: Border.all(
                              color: Colors.white.withValues(alpha: 0.25)),
                        ),
                        child: Icon(Icons.arrow_back_ios_new_rounded,
                            size: 15.r, color: Colors.white),
                      ),
                    ),
                    SizedBox(width: 4.w),
                    Text(
                      'BILLING REPORT',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 18.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 1.5,
                        height: 1.0,
                        color: Colors.white,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),

          // ── Body ──────────────────────────────────────────────────────────
          Expanded(
            child: BlocBuilder<OutletBillingsCubit, OutletBillingsState>(
              builder: (context, state) {
                if (state is OutletBillingsRoutesLoading) {
                  return const Center(child: AppSpinner());
                }

                if (state is OutletBillingsError) {
                  return Center(
                    child: Padding(
                      padding: EdgeInsets.all(32.r),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.error_outline_rounded,
                              size: 48.r, color: AppColors.error),
                          SizedBox(height: 12.h),
                          Text(
                            state.message,
                            textAlign: TextAlign.center,
                            style: GoogleFonts.barlow(
                              color: AppColors.foregroundMuted,
                              fontSize: 13.sp,
                            ),
                          ),
                          SizedBox(height: 16.h),
                          TextButton(
                            onPressed: () =>
                                context.read<OutletBillingsCubit>().load(),
                            child: Text('Retry',
                                style: GoogleFonts.barlowCondensed(
                                  fontSize: 14.sp,
                                  fontWeight: FontWeight.w700,
                                  color: AppColors.primary,
                                )),
                          ),
                        ],
                      ),
                    ),
                  );
                }

                if (state is! OutletBillingsLoaded) {
                  return const SizedBox.shrink();
                }

                final cubit = context.read<OutletBillingsCubit>();

                return CustomScrollView(
                  slivers: [
                    // ── Month chips ──────────────────────────────────────────
                    SliverToBoxAdapter(
                      child: MonthFilterChips(
                        selectedOffset: state.monthOffset,
                        labelBuilder: cubit.monthLabel,
                        onChanged: cubit.changeMonth,
                      ),
                    ),

                    // ── Routes section label ─────────────────────────────────
                    SliverToBoxAdapter(
                      child: Padding(
                        padding: EdgeInsets.fromLTRB(16.w, 4.h, 16.w, 8.h),
                        child: _SectionLabel(
                            'ROUTES — ${cubit.monthLabel(state.monthOffset).toUpperCase()}'),
                      ),
                    ),

                    if (state.availableRoutes.isEmpty)
                      SliverToBoxAdapter(
                        child: Padding(
                          padding: EdgeInsets.symmetric(
                              horizontal: 16.w, vertical: 24.h),
                          child: Text(
                            'No route assignments found for this period.',
                            style: GoogleFonts.barlow(
                              color: AppColors.foregroundMuted,
                              fontSize: 13.sp,
                            ),
                          ),
                        ),
                      )
                    else
                      SliverList.builder(
                        itemCount: state.availableRoutes.length,
                        itemBuilder: (context, i) {
                          final route = state.availableRoutes[i];
                          return RouteListTile(
                            route: route,
                            isSelected:
                                state.selectedRoute?.routeId == route.routeId,
                            onTap: () => cubit.selectRoute(route),
                          );
                        },
                      ),

                    // ── Outlet summary section ───────────────────────────────
                    if (state.selectedRoute != null) ...[
                      SliverToBoxAdapter(
                        child: Padding(
                          padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 0),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Divider(
                                  color: AppColors.surfaceVariant, height: 1),
                              SizedBox(height: 12.h),
                              _SectionLabel(
                                  state.selectedRoute!.routeName.toUpperCase()),
                              SizedBox(height: 12.h),
                              // Grand total bar
                              if (!state.loadingOutlets &&
                                  state.outletSummaries.isNotEmpty)
                                Container(
                                  padding: EdgeInsets.symmetric(
                                      horizontal: 16.w, vertical: 14.h),
                                  decoration: BoxDecoration(
                                    color: AppColors.darkSurface,
                                    borderRadius: BorderRadius.circular(10.r),
                                  ),
                                  child: Row(
                                    children: [
                                      Icon(Icons.receipt_long_rounded,
                                          size: 16.r, color: AppColors.amber),
                                      SizedBox(width: 8.w),
                                      Text(
                                        '${state.totalBillingCount} billings',
                                        style: GoogleFonts.barlowCondensed(
                                          fontSize: 14.sp,
                                          color: Colors.white
                                              .withValues(alpha: 0.75),
                                          fontWeight: FontWeight.w600,
                                          letterSpacing: 0.3,
                                        ),
                                      ),
                                      const Spacer(),
                                      Text(
                                        'Rs. ${_formatAmount(state.grandTotal)}',
                                        style: GoogleFonts.barlowCondensed(
                                          fontSize: 20.sp,
                                          color: AppColors.amber,
                                          fontWeight: FontWeight.w800,
                                          letterSpacing: -0.3,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              SizedBox(height: 8.h),
                            ],
                          ),
                        ),
                      ),

                      if (state.loadingOutlets)
                        const SliverToBoxAdapter(
                          child: Padding(
                            padding: EdgeInsets.symmetric(vertical: 32),
                            child: Center(child: AppSpinner()),
                          ),
                        )
                      else if (state.outletSummaries.isEmpty)
                        SliverToBoxAdapter(
                          child: Padding(
                            padding: EdgeInsets.symmetric(
                                horizontal: 16.w, vertical: 24.h),
                            child: Text(
                              'No billings found for this route in the selected period.',
                              style: GoogleFonts.barlow(
                                color: AppColors.foregroundMuted,
                                fontSize: 13.sp,
                              ),
                            ),
                          ),
                        )
                      else
                        SliverList.builder(
                          itemCount: state.outletSummaries.length,
                          itemBuilder: (_, i) => OutletBillingCard(
                              summary: state.outletSummaries[i]),
                        ),
                    ],

                    SliverToBoxAdapter(child: SizedBox(height: 40.h)),
                  ],
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  String _formatAmount(double amount) {
    return amount.toStringAsFixed(0).replaceAllMapped(
          RegExp(r'(\d)(?=(\d{3})+$)'),
          (m) => '${m[1]},',
        );
  }
}

class _SectionLabel extends StatelessWidget {
  final String text;
  const _SectionLabel(this.text);

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 3.w,
          height: 12.h,
          decoration: BoxDecoration(
            color: AppColors.primary,
            borderRadius: BorderRadius.circular(2.r),
          ),
        ),
        SizedBox(width: 8.w),
        Text(
          text,
          style: GoogleFonts.barlowCondensed(
            fontSize: 11.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 2.5,
            color: AppColors.foregroundMuted,
          ),
        ),
      ],
    );
  }
}
