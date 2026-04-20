import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/products/presentation/bloc/products_bloc.dart';
import 'package:uswatte/features/products/presentation/bloc/products_event.dart';
import 'package:uswatte/features/products/presentation/bloc/products_state.dart';

class SyncPage extends StatelessWidget {
  const SyncPage({super.key});

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return BlocBuilder<ProductsBloc, ProductsState>(
      builder: (context, productsState) {
        final isAnySyncing = _isAnySyncing(productsState);
        final allSynced = _isAllSynced(productsState);

        return Scaffold(
          backgroundColor: AppColors.background,
          body: CustomScrollView(
            slivers: [
              // ── App bar ──────────────────────────────────────────────────
              _SyncAppBar(onBack: () => context.pop()),

              // ── Status banner ────────────────────────────────────────────
              SliverToBoxAdapter(
                child: Padding(
                  padding: EdgeInsets.fromLTRB(16.w, 4.h, 16.w, 0),
                  child: _StatusBanner(
                    isAnySyncing: isAnySyncing,
                    allSynced: allSynced,
                  ),
                ),
              ),

              // ── Section label ────────────────────────────────────────────
              SliverToBoxAdapter(
                child: Padding(
                  padding: EdgeInsets.fromLTRB(20.w, 24.h, 20.w, 10.h),
                  child: Row(
                    children: [
                      Container(
                        width: 3.w,
                        height: 13.h,
                        decoration: BoxDecoration(
                          color: AppColors.primary,
                          borderRadius: BorderRadius.circular(2.r),
                        ),
                      ),
                      SizedBox(width: 8.w),
                      Text(
                        'DATA CATEGORIES',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 11.sp,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 2.5,
                          color: AppColors.foregroundMuted,
                        ),
                      ),
                    ],
                  ),
                ),
              ),

              // ── Category cards ───────────────────────────────────────────
              SliverPadding(
                padding: EdgeInsets.symmetric(horizontal: 16.w),
                sliver: SliverList.list(
                  children: [
                    _ProductsCategoryCard(
                      state: productsState,
                      onSync: () => context
                          .read<ProductsBloc>()
                          .add(const SyncProductsRequested()),
                      onView: () => context.push('/sales-rep/products'),
                    ),
                    // ── Future categories go here ──────────────────────────
                    // _OutletsCategoryCard(...)
                    // _StockCategoryCard(...)
                  ],
                ),
              ),

              // ── SYNC ALL button pinned to bottom ─────────────────────────
              SliverFillRemaining(
                hasScrollBody: false,
                child: Align(
                  alignment: Alignment.bottomCenter,
                  child: Padding(
                    padding: EdgeInsets.fromLTRB(16.w, 24.h, 16.w, 36.h),
                    child: _SyncAllButton(
                      isSyncing: isAnySyncing,
                      onTap: isAnySyncing
                          ? null
                          : () => context
                              .read<ProductsBloc>()
                              .add(const SyncProductsRequested()),
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  static bool _isAnySyncing(ProductsState s) {
    if (s is ProductsLoading) return true;
    if (s is ProductsLoaded) return s.isSyncing;
    return false;
  }

  static bool _isAllSynced(ProductsState s) =>
      s is ProductsLoaded && s.lastSyncedAt != null && !s.isSyncing;
}

// ── App bar ───────────────────────────────────────────────────────────────────

class _SyncAppBar extends StatelessWidget {
  const _SyncAppBar({required this.onBack});
  final VoidCallback onBack;

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
            padding: EdgeInsets.fromLTRB(8.w, 4.h, 8.w, 16.h),
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
                Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'DATA SYNC',
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
                      'Keep your device data up to date',
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: Colors.white.withValues(alpha: 0.70),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// ── Status banner ─────────────────────────────────────────────────────────────

class _StatusBanner extends StatelessWidget {
  const _StatusBanner({
    required this.isAnySyncing,
    required this.allSynced,
  });

  final bool isAnySyncing;
  final bool allSynced;

  @override
  Widget build(BuildContext context) {
    final Color color;
    final IconData icon;
    final String label;

    if (isAnySyncing) {
      color = AppColors.primary;
      icon = Icons.sync_rounded;
      label = 'Syncing data…';
    } else if (allSynced) {
      color = AppColors.success;
      icon = Icons.check_circle_outline_rounded;
      label = 'All data is up to date';
    } else {
      color = AppColors.warning;
      icon = Icons.warning_amber_rounded;
      label = 'Some data has never been synced';
    }

    return Container(
      padding: EdgeInsets.symmetric(horizontal: 14.w, vertical: 10.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(10.r),
        border: Border.all(color: color.withValues(alpha: 0.20)),
      ),
      child: Row(
        children: [
          isAnySyncing
              ? SizedBox(
                  width: 16.r,
                  height: 16.r,
                  child: CircularProgressIndicator(
                      strokeWidth: 1.5, color: color),
                )
              : Icon(icon, size: 16.r, color: color),
          SizedBox(width: 10.w),
          Text(
            label,
            style: GoogleFonts.barlow(
              fontSize: 12.sp,
              fontWeight: FontWeight.w600,
              color: color,
            ),
          ),
        ],
      ),
    );
  }
}

// ── Products category card ────────────────────────────────────────────────────

class _ProductsCategoryCard extends StatelessWidget {
  const _ProductsCategoryCard({
    required this.state,
    required this.onSync,
    required this.onView,
  });

  final ProductsState state;
  final VoidCallback onSync;
  final VoidCallback onView;

  @override
  Widget build(BuildContext context) {
    final int? count = state is ProductsLoaded
        ? (state as ProductsLoaded).products.length
        : null;
    final DateTime? lastSyncedAt = state is ProductsLoaded
        ? (state as ProductsLoaded).lastSyncedAt
        : null;
    final bool isSyncing =
        state is ProductsLoading || (state is ProductsLoaded && (state as ProductsLoaded).isSyncing);
    final bool hasError = state is ProductsError;

    return _CategoryCard(
      icon: Icons.inventory_2_rounded,
      label: 'PRODUCTS CATALOG',
      subtitle: 'All active products',
      accentColor: AppColors.primary,
      itemCount: count,
      itemUnit: 'products',
      lastSyncedAt: lastSyncedAt,
      isSyncing: isSyncing,
      hasError: hasError,
      errorMessage: hasError ? (state as ProductsError).message : null,
      onSync: onSync,
      onView: onView,
    );
  }
}

// ── Generic category card ─────────────────────────────────────────────────────

class _CategoryCard extends StatelessWidget {
  const _CategoryCard({
    required this.icon,
    required this.label,
    required this.subtitle,
    required this.accentColor,
    required this.itemCount,
    required this.itemUnit,
    required this.lastSyncedAt,
    required this.isSyncing,
    required this.hasError,
    required this.errorMessage,
    required this.onSync,
    required this.onView,
  });

  final IconData icon;
  final String label;
  final String subtitle;
  final Color accentColor;
  final int? itemCount;
  final String itemUnit;
  final DateTime? lastSyncedAt;
  final bool isSyncing;
  final bool hasError;
  final String? errorMessage;
  final VoidCallback onSync;
  final VoidCallback onView;

  String _syncLabel() {
    if (isSyncing) return 'Syncing…';
    if (hasError) return errorMessage ?? 'Sync failed';
    if (lastSyncedAt == null) return 'Never synced';
    final diff = DateTime.now().difference(lastSyncedAt!);
    if (diff.inMinutes < 1) return 'Just now';
    if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
    if (diff.inHours < 24) return '${diff.inHours}h ago';
    return '${diff.inDays}d ago';
  }

  Color _statusColor() {
    if (isSyncing) return accentColor;
    if (hasError) return AppColors.error;
    if (lastSyncedAt == null) return AppColors.warning;
    return AppColors.success;
  }

  IconData _statusIcon() {
    if (hasError) return Icons.error_outline_rounded;
    if (lastSyncedAt == null) return Icons.cloud_off_rounded;
    return Icons.check_circle_outline_rounded;
  }

  @override
  Widget build(BuildContext context) {
    final statusColor = _statusColor();

    return Container(
      margin: EdgeInsets.only(bottom: 10.h),
      padding: EdgeInsets.all(16.r),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14.r),
        border: Border.all(color: AppColors.surfaceVariant),
        boxShadow: [
          BoxShadow(
            color: AppColors.foreground.withValues(alpha: 0.04),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Header row ────────────────────────────────────────────────
          Row(
            children: [
              Container(
                width: 38.r,
                height: 38.r,
                decoration: BoxDecoration(
                  color: accentColor.withValues(alpha: 0.10),
                  borderRadius: BorderRadius.circular(10.r),
                ),
                child: Icon(icon, size: 18.r, color: accentColor),
              ),
              SizedBox(width: 12.w),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      label,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 15.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 0.8,
                        height: 1.0,
                        color: AppColors.foreground,
                      ),
                    ),
                    Text(
                      subtitle,
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: AppColors.foregroundMuted,
                      ),
                    ),
                  ],
                ),
              ),
              // Sync button
              GestureDetector(
                onTap: isSyncing ? null : onSync,
                child: Container(
                  width: 34.r,
                  height: 34.r,
                  decoration: BoxDecoration(
                    color: accentColor.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(8.r),
                    border: Border.all(
                        color: accentColor.withValues(alpha: 0.20)),
                  ),
                  child: isSyncing
                      ? Padding(
                          padding: EdgeInsets.all(9.r),
                          child: CircularProgressIndicator(
                              strokeWidth: 1.5, color: accentColor),
                        )
                      : Icon(Icons.sync_rounded,
                          size: 15.r, color: accentColor),
                ),
              ),
            ],
          ),

          SizedBox(height: 14.h),
          Container(height: 1, color: AppColors.surfaceVariant),
          SizedBox(height: 12.h),

          // ── Stats row ─────────────────────────────────────────────────
          Row(
            children: [
              // Item count
              Expanded(
                child: Row(
                  children: [
                    Container(
                      width: 28.r,
                      height: 28.r,
                      decoration: BoxDecoration(
                        color: AppColors.surface,
                        borderRadius: BorderRadius.circular(6.r),
                      ),
                      child: Icon(Icons.tag_rounded,
                          size: 13.r, color: AppColors.foregroundMuted),
                    ),
                    SizedBox(width: 8.w),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          itemCount != null ? '$itemCount' : '—',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 20.sp,
                            fontWeight: FontWeight.w900,
                            height: 1.0,
                            letterSpacing: -0.5,
                            color: AppColors.foreground,
                          ),
                        ),
                        Text(
                          itemUnit.toUpperCase(),
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 8.sp,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 1.0,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),

              // Status
              Container(
                padding:
                    EdgeInsets.symmetric(horizontal: 10.w, vertical: 6.h),
                decoration: BoxDecoration(
                  color: statusColor.withValues(alpha: 0.08),
                  borderRadius: BorderRadius.circular(20.r),
                  border: Border.all(
                      color: statusColor.withValues(alpha: 0.20)),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    isSyncing
                        ? SizedBox(
                            width: 10.r,
                            height: 10.r,
                            child: CircularProgressIndicator(
                                strokeWidth: 1.2, color: statusColor),
                          )
                        : Icon(_statusIcon(),
                            size: 11.r, color: statusColor),
                    SizedBox(width: 5.w),
                    Text(
                      _syncLabel(),
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 11.sp,
                        fontWeight: FontWeight.w700,
                        letterSpacing: 0.3,
                        color: statusColor,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),

          SizedBox(height: 12.h),

          // ── View list link ────────────────────────────────────────────
          GestureDetector(
            onTap: onView,
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  'View catalog',
                  style: GoogleFonts.barlow(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w600,
                    color: accentColor,
                  ),
                ),
                SizedBox(width: 4.w),
                Icon(Icons.arrow_forward_ios_rounded,
                    size: 10.r, color: accentColor),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Sync all button ───────────────────────────────────────────────────────────

class _SyncAllButton extends StatelessWidget {
  const _SyncAllButton({required this.isSyncing, required this.onTap});

  final bool isSyncing;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12.r),
        child: Ink(
          width: double.infinity,
          height: 56.h,
          decoration: BoxDecoration(
            color: isSyncing
                ? AppColors.primary.withValues(alpha: 0.50)
                : AppColors.primary,
            borderRadius: BorderRadius.circular(12.r),
            boxShadow: isSyncing
                ? null
                : [
                    BoxShadow(
                      color: AppColors.primary.withValues(alpha: 0.30),
                      blurRadius: 14,
                      offset: const Offset(0, 5),
                    ),
                  ],
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              isSyncing
                  ? SizedBox(
                      width: 16.r,
                      height: 16.r,
                      child: CircularProgressIndicator(
                          strokeWidth: 1.8, color: Colors.white),
                    )
                  : Icon(Icons.sync_rounded,
                      size: 18.r, color: Colors.white),
              SizedBox(width: 10.w),
              Text(
                isSyncing ? 'SYNCING…' : 'SYNC ALL',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 17.sp,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 2.0,
                  color: Colors.white,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
