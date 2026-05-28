import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';
import 'package:uswatte/features/item_wise_achievement/presentation/cubit/item_wise_achievement_cubit.dart';
import 'package:uswatte/features/item_wise_achievement/presentation/cubit/item_wise_achievement_state.dart';

const _greenAccent = Color(0xFF22C55E);
const _amberAccent = Color(0xFFF59E0B);

Color _accentForPercent(double pct) {
  if (pct >= 100) return _greenAccent;
  if (pct >= 75) return _amberAccent;
  return AppColors.primary;
}

class ItemWiseAchievementPage extends StatelessWidget {
  const ItemWiseAchievementPage({super.key});

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    final now = DateTime.now();

    return Scaffold(
      backgroundColor: AppColors.surface,
      body: Column(
        children: [
          _AppBar(
            year: now.year,
            month: now.month,
            onBack: () => context.pop(),
          ),
          Expanded(
            child: BlocBuilder<ItemWiseAchievementCubit, ItemWiseAchievementState>(
              builder: (ctx, state) {
                return switch (state) {
                  ItemWiseAchievementInitial() ||
                  ItemWiseAchievementLoading() =>
                    const Center(child: AppSpinner()),
                  ItemWiseAchievementErrorState(:final message) => _ErrorView(
                      message: message,
                      onRetry: () => ctx
                          .read<ItemWiseAchievementCubit>()
                          .load(now.year, now.month),
                    ),
                  ItemWiseAchievementLoaded(:final data) => RefreshIndicator(
                      color: AppColors.primary,
                      onRefresh: () => ctx
                          .read<ItemWiseAchievementCubit>()
                          .load(now.year, now.month),
                      child: data.items.isEmpty
                          ? ListView(
                              physics: const AlwaysScrollableScrollPhysics(),
                              children: [
                                _SummaryStrip(data: data),
                                SizedBox(height: 80.h),
                                const _EmptyView(),
                              ],
                            )
                          : CustomScrollView(
                              physics: const AlwaysScrollableScrollPhysics(),
                              slivers: [
                                SliverToBoxAdapter(
                                  child: _SummaryStrip(data: data),
                                ),
                                SliverPadding(
                                  padding:
                                      EdgeInsets.fromLTRB(16.w, 4.h, 16.w, 40.h),
                                  sliver: SliverList.separated(
                                    itemCount: data.items.length,
                                    separatorBuilder: (_, __) =>
                                        SizedBox(height: 10.h),
                                    itemBuilder: (_, i) =>
                                        _ItemRow(item: data.items[i]),
                                  ),
                                ),
                              ],
                            ),
                    ),
                };
              },
            ),
          ),
        ],
      ),
    );
  }
}

// ── App bar ───────────────────────────────────────────────────────────────────

class _AppBar extends StatelessWidget {
  final int year;
  final int month;
  final VoidCallback onBack;

  const _AppBar({
    required this.year,
    required this.month,
    required this.onBack,
  });

  static const _months = [
    'JANUARY','FEBRUARY','MARCH','APRIL','MAY','JUNE',
    'JULY','AUGUST','SEPTEMBER','OCTOBER','NOVEMBER','DECEMBER',
  ];

  @override
  Widget build(BuildContext context) {
    return Container(
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
              SizedBox(width: 8.w),
              Expanded(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'ITEM-WISE ACHIEVEMENT',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 18.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 1.5,
                        height: 1.0,
                        color: Colors.white,
                      ),
                    ),
                    SizedBox(height: 2.h),
                    Text(
                      '${_months[month - 1]} $year',
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: Colors.white.withValues(alpha: 0.75),
                        letterSpacing: 0.5,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Summary strip ─────────────────────────────────────────────────────────────

class _SummaryStrip extends StatelessWidget {
  final ItemWiseAchievement data;
  const _SummaryStrip({required this.data});

  @override
  Widget build(BuildContext context) {
    final totalPct = data.totalTargetQuantity > 0
        ? (data.totalSoldQuantity / data.totalTargetQuantity * 100)
            .clamp(0.0, 9999.0)
        : 0.0;
    final accent = _accentForPercent(totalPct);

    return Container(
      color: Colors.white,
      padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 14.h),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Expanded(
            child: _MiniStat(
              label: 'TARGET',
              value: '${_fmtCases(data.totalTargetQuantity)} CS',
            ),
          ),
          Container(
            width: 1,
            height: 30.h,
            color: AppColors.surfaceVariant,
          ),
          Expanded(
            child: _MiniStat(
              label: 'SOLD',
              value:
                  '${_fmtCases(data.totalSoldQuantity)} CS · ${_fmtPacks(data.totalSoldQuantityPacks)} PKT',
            ),
          ),
          Container(
            width: 1,
            height: 30.h,
            color: AppColors.surfaceVariant,
          ),
          Expanded(
            child: Center(
              child: Container(
                padding:
                    EdgeInsets.symmetric(horizontal: 10.w, vertical: 5.h),
                decoration: BoxDecoration(
                  color: accent.withValues(alpha: 0.10),
                  borderRadius: BorderRadius.circular(999),
                  border: Border.all(color: accent.withValues(alpha: 0.30)),
                ),
                child: Text(
                  '${totalPct.toStringAsFixed(0)}%',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 16.sp,
                    fontWeight: FontWeight.w900,
                    letterSpacing: 0.5,
                    color: accent,
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _MiniStat extends StatelessWidget {
  final String label;
  final String value;
  const _MiniStat({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          label,
          style: GoogleFonts.barlowCondensed(
            fontSize: 9.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.5,
            color: AppColors.foregroundMuted,
          ),
        ),
        SizedBox(height: 3.h),
        Text(
          value,
          style: GoogleFonts.barlowCondensed(
            fontSize: 16.sp,
            fontWeight: FontWeight.w800,
            color: AppColors.foreground,
          ),
        ),
      ],
    );
  }
}

// ── Item row ──────────────────────────────────────────────────────────────────

class _ItemRow extends StatelessWidget {
  final ItemAchievement item;
  const _ItemRow({required this.item});

  @override
  Widget build(BuildContext context) {
    final pct = item.achievementPercent;
    final accent = _accentForPercent(pct);
    final ringValue = (pct / 100).clamp(0.0, 1.0);

    return Container(
      padding: EdgeInsets.all(12.r),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14.r),
        border: Border.all(color: accent.withValues(alpha: 0.18)),
        boxShadow: [
          BoxShadow(
            color: AppColors.foreground.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          // Ring
          SizedBox(
            width: 50.r,
            height: 50.r,
            child: Stack(
              alignment: Alignment.center,
              children: [
                SizedBox(
                  width: 50.r,
                  height: 50.r,
                  child: CircularProgressIndicator(
                    value: ringValue,
                    strokeWidth: 4.r,
                    backgroundColor: accent.withValues(alpha: 0.12),
                    valueColor: AlwaysStoppedAnimation<Color>(accent),
                    strokeCap: StrokeCap.round,
                  ),
                ),
                Text(
                  '${pct.toStringAsFixed(0)}%',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w900,
                    height: 1.0,
                    color: accent,
                  ),
                ),
              ],
            ),
          ),
          SizedBox(width: 12.w),

          // Code + name
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  item.itemCode,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 11.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 1.0,
                    color: AppColors.foregroundMuted,
                  ),
                ),
                SizedBox(height: 2.h),
                Text(
                  item.itemName,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 14.sp,
                    fontWeight: FontWeight.w700,
                    height: 1.15,
                    color: AppColors.foreground,
                  ),
                ),
                SizedBox(height: 6.h),
                Text(
                  'Rs. ${_fmtAmount(item.soldAmount)}',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w700,
                    color: AppColors.amber,
                  ),
                ),
              ],
            ),
          ),

          SizedBox(width: 10.w),

          // Target / Sold qty stack — Target only in cases; Sold shown in CS · PKT
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            mainAxisSize: MainAxisSize.min,
            children: [
              _QtyChip(
                label: 'TARGET',
                value: '${_fmtCases(item.targetQuantity)} CS',
                color: AppColors.foregroundMuted,
              ),
              SizedBox(height: 6.h),
              _SoldChip(
                cases: item.soldQuantity,
                packs: item.soldQuantityPacks,
                color: accent,
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _QtyChip extends StatelessWidget {
  final String label;
  final String value;
  final Color color;

  const _QtyChip({
    required this.label,
    required this.value,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          label,
          style: GoogleFonts.barlowCondensed(
            fontSize: 9.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.5,
            color: AppColors.foregroundMuted,
          ),
        ),
        Text(
          value,
          style: GoogleFonts.barlowCondensed(
            fontSize: 14.sp,
            fontWeight: FontWeight.w800,
            height: 1.1,
            color: color,
          ),
        ),
      ],
    );
  }
}

class _SoldChip extends StatelessWidget {
  final double cases;
  final double packs;
  final Color color;

  const _SoldChip({
    required this.cases,
    required this.packs,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          'SOLD',
          style: GoogleFonts.barlowCondensed(
            fontSize: 9.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.5,
            color: AppColors.foregroundMuted,
          ),
        ),
        Text(
          '${_fmtCases(cases)} CS',
          style: GoogleFonts.barlowCondensed(
            fontSize: 14.sp,
            fontWeight: FontWeight.w800,
            height: 1.1,
            color: color,
          ),
        ),
        Text(
          '${_fmtPacks(packs)} PKT',
          style: GoogleFonts.barlowCondensed(
            fontSize: 11.sp,
            fontWeight: FontWeight.w600,
            height: 1.2,
            color: AppColors.foregroundMuted,
          ),
        ),
      ],
    );
  }
}

// ── Empty / error ─────────────────────────────────────────────────────────────

class _EmptyView extends StatelessWidget {
  const _EmptyView();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.inventory_2_outlined,
                size: 48.r, color: AppColors.surfaceVariant),
            SizedBox(height: 16.h),
            Text(
              'No targets or sales',
              style: GoogleFonts.barlowCondensed(
                fontSize: 18.sp,
                fontWeight: FontWeight.w700,
                color: AppColors.foreground,
              ),
            ),
            SizedBox(height: 6.h),
            Text(
              'Nothing recorded for the current month yet.',
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(
                  fontSize: 13.sp, color: AppColors.foregroundMuted),
            ),
          ],
        ),
      ),
    );
  }
}

class _ErrorView extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;

  const _ErrorView({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.cloud_off_rounded,
                size: 48.r, color: AppColors.foregroundMuted),
            SizedBox(height: 16.h),
            Text(
              'Could not load report',
              style: GoogleFonts.barlowCondensed(
                fontSize: 18.sp,
                fontWeight: FontWeight.w700,
                color: AppColors.foreground,
              ),
            ),
            SizedBox(height: 6.h),
            Text(
              message,
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(
                  fontSize: 12.sp, color: AppColors.foregroundMuted),
            ),
            SizedBox(height: 20.h),
            TextButton.icon(
              onPressed: onRetry,
              icon: Icon(Icons.refresh_rounded, size: 16.r),
              label: Text('Retry', style: GoogleFonts.barlow(fontSize: 14.sp)),
              style: TextButton.styleFrom(foregroundColor: AppColors.primary),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Formatting helpers ────────────────────────────────────────────────────────

// Cases: max 1 decimal, no trailing ".0" for whole numbers.
String _fmtCases(double v) {
  if (v == v.roundToDouble()) return v.toStringAsFixed(0);
  return v.toStringAsFixed(1);
}

// Packs: always whole, with thousands separators for readability.
String _fmtPacks(double v) {
  final whole = v.round().toString();
  final buf = StringBuffer();
  for (int i = 0; i < whole.length; i++) {
    if (i > 0 && (whole.length - i) % 3 == 0) buf.write(',');
    buf.write(whole[i]);
  }
  return buf.toString();
}

String _fmtAmount(double v) {
  final s = v.toStringAsFixed(2);
  final parts = s.split('.');
  final whole = parts[0];
  final buf = StringBuffer();
  for (int i = 0; i < whole.length; i++) {
    if (i > 0 && (whole.length - i) % 3 == 0) buf.write(',');
    buf.write(whole[i]);
  }
  return '$buf.${parts[1]}';
}
