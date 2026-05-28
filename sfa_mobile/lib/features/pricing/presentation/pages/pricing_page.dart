import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';
import 'package:uswatte/features/pricing/presentation/bloc/pricing_bloc.dart';
import 'package:uswatte/features/pricing/presentation/bloc/pricing_event.dart';
import 'package:uswatte/features/pricing/presentation/bloc/pricing_state.dart';

class PricingPage extends StatelessWidget {
  const PricingPage({super.key});

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return BlocBuilder<PricingBloc, PricingState>(
      builder: (context, state) {
        return Scaffold(
          backgroundColor: AppColors.background,
          body: CustomScrollView(
            slivers: [
              _PricingAppBar(
                onBack: () => context.pop(),
                isSyncing: state is PricingLoaded && state.isSyncing,
                onRefresh: () => context
                    .read<PricingBloc>()
                    .add(const SyncPricingRequested()),
              ),
              if (state is PricingLoading)
                const SliverFillRemaining(
                  child: Center(child: AppSpinner()),
                )
              else if (state is PricingError)
                SliverFillRemaining(
                  child: Center(
                    child: Padding(
                      padding: EdgeInsets.all(24.r),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.cloud_off_rounded,
                              size: 40.r, color: AppColors.foregroundMuted),
                          SizedBox(height: 12.h),
                          Text(
                            state.message,
                            textAlign: TextAlign.center,
                            style: GoogleFonts.barlow(
                              fontSize: 13.sp,
                              color: AppColors.foregroundMuted,
                            ),
                          ),
                          SizedBox(height: 16.h),
                          TextButton(
                            onPressed: () => context
                                .read<PricingBloc>()
                                .add(const SyncPricingRequested()),
                            child: const Text('Retry'),
                          ),
                        ],
                      ),
                    ),
                  ),
                )
              else if (state is PricingLoaded && state.structures.isEmpty)
                SliverFillRemaining(
                  child: Center(
                    child: Padding(
                      padding: EdgeInsets.all(24.r),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.price_change_rounded,
                              size: 40.r, color: AppColors.foregroundMuted),
                          SizedBox(height: 12.h),
                          Text(
                            'No pricing synced yet.\nGo to Sync Data to download the price list.',
                            textAlign: TextAlign.center,
                            style: GoogleFonts.barlow(
                              fontSize: 13.sp,
                              color: AppColors.foregroundMuted,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                )
              else if (state is PricingLoaded)
                SliverPadding(
                  padding:
                      EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
                  sliver: SliverList.separated(
                    itemCount: state.structures.length,
                    separatorBuilder: (_, __) => SizedBox(height: 10.h),
                    itemBuilder: (context, index) => _StructureCard(
                      structure: state.structures[index],
                      onTap: () => context.push(
                        '/sales-rep/pricing/detail',
                        extra: state.structures[index],
                      ),
                    ),
                  ),
                ),
              SliverToBoxAdapter(child: SizedBox(height: 24.h)),
            ],
          ),
        );
      },
    );
  }
}

// ── App bar ───────────────────────────────────────────────────────────────────

class _PricingAppBar extends StatelessWidget {
  const _PricingAppBar({
    required this.onBack,
    required this.isSyncing,
    required this.onRefresh,
  });

  final VoidCallback onBack;
  final bool isSyncing;
  final VoidCallback onRefresh;

  @override
  Widget build(BuildContext context) {
    return SliverToBoxAdapter(
      child: Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [AppColors.primaryDark, AppColors.primary],
          ),
        ),
        child: SafeArea(
          bottom: false,
          child: Padding(
            padding: EdgeInsets.fromLTRB(8.w, 4.h, 16.w, 16.h),
            child: Row(
              children: [
                GestureDetector(
                  onTap: onBack,
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
                Expanded(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'PRICING STRUCTURES',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 18.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.5,
                          height: 1.0,
                          color: Colors.white,
                        ),
                      ),
                      SizedBox(height: 2.r),
                      Text(
                        'Select a structure to view prices',
                        style: GoogleFonts.barlow(
                          fontSize: 11.sp,
                          color: Colors.white.withValues(alpha: 0.70),
                        ),
                      ),
                    ],
                  ),
                ),
                GestureDetector(
                  onTap: isSyncing ? null : onRefresh,
                  child: isSyncing
                      ? const AppSpinner.small(color: Colors.white)
                      : Icon(Icons.sync_rounded,
                          size: 20.r, color: Colors.white),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// ── Structure selector card ───────────────────────────────────────────────────

class _StructureCard extends StatelessWidget {
  const _StructureCard({required this.structure, required this.onTap});

  final PricingStructure structure;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: EdgeInsets.all(16.r),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12.r),
          border: Border.all(color: AppColors.surfaceVariant),
          boxShadow: [
            BoxShadow(
              color: AppColors.foreground.withValues(alpha: 0.03),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Row(
          children: [
            Container(
              width: 42.r,
              height: 42.r,
              decoration: BoxDecoration(
                color: AppColors.success.withValues(alpha: 0.10),
                borderRadius: BorderRadius.circular(10.r),
              ),
              child: Icon(Icons.price_change_rounded,
                  size: 20.r, color: AppColors.success),
            ),
            SizedBox(width: 14.w),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          structure.name,
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 16.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 0.3,
                            height: 1.1,
                            color: AppColors.foreground,
                          ),
                        ),
                      ),
                      if (structure.isDefault)
                        Container(
                          padding: EdgeInsets.symmetric(
                              horizontal: 7.w, vertical: 3.h),
                          decoration: BoxDecoration(
                            color: AppColors.success.withValues(alpha: 0.10),
                            borderRadius: BorderRadius.circular(4.r),
                            border: Border.all(
                                color:
                                    AppColors.success.withValues(alpha: 0.25)),
                          ),
                          child: Text(
                            'DEFAULT',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 9.sp,
                              fontWeight: FontWeight.w800,
                              letterSpacing: 0.8,
                              color: AppColors.success,
                            ),
                          ),
                        ),
                    ],
                  ),
                  SizedBox(height: 3.h),
                  Text(
                    '${structure.items.length} products with prices',
                    style: GoogleFonts.barlow(
                      fontSize: 11.sp,
                      color: AppColors.foregroundMuted,
                    ),
                  ),
                ],
              ),
            ),
            SizedBox(width: 8.w),
            Icon(Icons.chevron_right_rounded,
                size: 20.r, color: AppColors.foregroundMuted),
          ],
        ),
      ),
    );
  }
}
