import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_bloc.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_event.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_state.dart';

class OutletsPage extends StatelessWidget {
  const OutletsPage({super.key});

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return BlocBuilder<OutletsBloc, OutletsState>(
      builder: (context, state) {
        return Scaffold(
          backgroundColor: AppColors.background,
          floatingActionButton: state is OutletsLoaded && state.hasActiveAssignment
              ? FloatingActionButton.extended(
                  onPressed: () => context.pushNamed('createOutlet'),
                  backgroundColor: AppColors.primary,
                  elevation: 3,
                  icon: Icon(Icons.add_rounded,
                      color: AppColors.onPrimary, size: 20.r),
                  label: Text(
                    'ADD OUTLET',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 1.0,
                      color: AppColors.onPrimary,
                    ),
                  ),
                )
              : null,
          body: CustomScrollView(
            slivers: [
              _OutletsAppBar(
                onBack: () => context.pop(),
                isSyncing: state is OutletsLoaded && state.isSyncing,
                onRefresh: () => context
                    .read<OutletsBloc>()
                    .add(const LoadOutletsRequested()),
              ),
              if (state is OutletsLoaded &&
                  !state.hasActiveAssignment &&
                  state.outlets.isNotEmpty)
                SliverToBoxAdapter(
                  child: _NoAssignmentBanner(lastSyncedAt: state.lastSyncedAt),
                ),
              if (state is OutletsLoading)
                const SliverFillRemaining(
                  child: Center(child: CircularProgressIndicator()),
                )
              else if (state is OutletsError)
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
                                .read<OutletsBloc>()
                                .add(const LoadOutletsRequested()),
                            child: const Text('Retry'),
                          ),
                        ],
                      ),
                    ),
                  ),
                )
              else if (state is OutletsLoaded && state.outlets.isEmpty)
                SliverFillRemaining(
                  child: Center(
                    child: Padding(
                      padding: EdgeInsets.all(24.r),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.storefront_outlined,
                              size: 40.r, color: AppColors.foregroundMuted),
                          SizedBox(height: 12.h),
                          Text(
                            'No outlets synced yet.\nGo to Sync Data to download today\'s outlets.',
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
              else if (state is OutletsLoaded)
                SliverPadding(
                  padding:
                      EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
                  sliver: SliverList.separated(
                    itemCount: state.outlets.length,
                    separatorBuilder: (_, __) => SizedBox(height: 8.h),
                    itemBuilder: (context, index) =>
                        _OutletCard(outlet: state.outlets[index]),
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

// ── No-assignment banner ──────────────────────────────────────────────────────

class _NoAssignmentBanner extends StatelessWidget {
  const _NoAssignmentBanner({this.lastSyncedAt});
  final DateTime? lastSyncedAt;

  String _syncLabel() {
    if (lastSyncedAt == null) return 'Showing last synced data';
    final diff = DateTime.now().difference(lastSyncedAt!);
    if (diff.inMinutes < 1) return 'Showing last synced data · just now';
    if (diff.inHours < 24) return 'Showing last synced data · ${diff.inHours}h ago';
    return 'Showing last synced data · ${diff.inDays}d ago';
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 4.h),
      padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 10.h),
      decoration: BoxDecoration(
        color: AppColors.amber.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(10.r),
        border: Border.all(color: AppColors.amber.withValues(alpha: 0.25)),
      ),
      child: Row(
        children: [
          Icon(Icons.info_outline_rounded, size: 15.r, color: AppColors.amber),
          SizedBox(width: 8.w),
          Expanded(
            child: Text(
              'No route assigned today · ${_syncLabel()}',
              style: GoogleFonts.barlow(
                fontSize: 11.sp,
                fontWeight: FontWeight.w600,
                color: AppColors.amber,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ── App bar ───────────────────────────────────────────────────────────────────

class _OutletsAppBar extends StatelessWidget {
  const _OutletsAppBar({
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
                        "TODAY'S OUTLETS",
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
                        'Outlets on your assigned route',
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
                      ? SizedBox(
                          width: 20.r,
                          height: 20.r,
                          child: CircularProgressIndicator(
                              strokeWidth: 1.5, color: Colors.white),
                        )
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

// ── Outlet card ───────────────────────────────────────────────────────────────

class _OutletCard extends StatelessWidget {
  const _OutletCard({required this.outlet});
  final Outlet outlet;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(14.r),
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
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 38.r,
            height: 38.r,
            decoration: BoxDecoration(
              color: AppColors.amber.withValues(alpha: 0.10),
              borderRadius: BorderRadius.circular(10.r),
            ),
            child:
                Icon(Icons.storefront_rounded, size: 18.r, color: AppColors.amber),
          ),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  outlet.name,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 15.sp,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 0.3,
                    height: 1.1,
                    color: AppColors.foreground,
                  ),
                ),
                SizedBox(height: 3.h),
                Text(
                  outlet.address,
                  style: GoogleFonts.barlow(
                    fontSize: 11.sp,
                    color: AppColors.foregroundMuted,
                  ),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                SizedBox(height: 6.h),
                Row(
                  children: [
                    _TypeBadge(outlet.outletType),
                    SizedBox(width: 6.w),
                    _TypeBadge(outlet.outletCategory,
                        color: AppColors.primary),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _TypeBadge extends StatelessWidget {
  const _TypeBadge(this.label, {this.color});
  final String label;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final c = color ?? AppColors.amber;
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 7.w, vertical: 3.h),
      decoration: BoxDecoration(
        color: c.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(4.r),
        border: Border.all(color: c.withValues(alpha: 0.25)),
      ),
      child: Text(
        label.toUpperCase(),
        style: GoogleFonts.barlowCondensed(
          fontSize: 9.sp,
          fontWeight: FontWeight.w700,
          letterSpacing: 0.8,
          color: c,
        ),
      ),
    );
  }
}
