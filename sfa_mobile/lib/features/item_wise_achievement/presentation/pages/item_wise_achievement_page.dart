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
                              padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 0),
                              children: [
                                _SummaryCard(data: data),
                                SizedBox(height: 80.h),
                                const _EmptyView(),
                              ],
                            )
                          : _ItemList(data: data),
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

// ── Item list ─────────────────────────────────────────────────────────────────

/// Items with a target come first (laggards first, as the API orders them),
/// then the items that sold without one under their own label.
class _ItemList extends StatelessWidget {
  final ItemWiseAchievement data;
  const _ItemList({required this.data});

  @override
  Widget build(BuildContext context) {
    final targeted = data.items.where((i) => i.hasTarget).toList();
    final untargeted = data.items.where((i) => !i.hasTarget).toList();

    final children = <Widget>[
      _SummaryCard(data: data),
      if (targeted.isNotEmpty) const _GroupLabel('ITEMS WITH A TARGET'),
      for (final item in targeted) _ItemCard(item: item),
      if (untargeted.isNotEmpty) const _GroupLabel('SOLD WITHOUT A TARGET'),
      for (final item in untargeted) _ItemCard(item: item),
      const _Footnote(),
    ];

    return ListView.separated(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 40.h),
      itemCount: children.length,
      separatorBuilder: (_, __) => SizedBox(height: 10.h),
      itemBuilder: (_, i) => children[i],
    );
  }
}

class _GroupLabel extends StatelessWidget {
  final String text;
  const _GroupLabel(this.text);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(top: 6.h, left: 2.w),
      child: Row(
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
              letterSpacing: 2.0,
              color: AppColors.foregroundMuted,
            ),
          ),
        ],
      ),
    );
  }
}

class _Footnote extends StatelessWidget {
  const _Footnote();

  @override
  Widget build(BuildContext context) {
    return Text(
      'Sold = distributor-approved bills minus returns. Pending bills count '
      'once approved; free issue is never counted as sold.',
      style: GoogleFonts.barlow(
        fontSize: 10.sp,
        color: AppColors.foregroundMuted,
      ),
    );
  }
}

// ── Summary card ──────────────────────────────────────────────────────────────

class _SummaryCard extends StatelessWidget {
  final ItemWiseAchievement data;
  const _SummaryCard({required this.data});

  @override
  Widget build(BuildContext context) {
    final pct = data.overallAchievementPercent;
    final hasAnyTarget = data.totalTargetQuantity > 0;
    final bar = (pct / 100).clamp(0.0, 1.0);

    return Container(
      padding: EdgeInsets.all(18.r),
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
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'OVERALL ACHIEVEMENT · ITEMS WITH A TARGET',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.barlowCondensed(
              fontSize: 11.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.8,
              color: Colors.white.withValues(alpha: 0.78),
            ),
          ),
          SizedBox(height: 2.h),
          Text(
            hasAnyTarget ? '${pct.toStringAsFixed(1)}%' : '—',
            style: GoogleFonts.barlowCondensed(
              fontSize: 40.sp,
              fontWeight: FontWeight.w900,
              height: 1.05,
              letterSpacing: -0.5,
              color: Colors.white,
            ),
          ),
          SizedBox(height: 8.h),
          ClipRRect(
            borderRadius: BorderRadius.circular(99),
            child: LinearProgressIndicator(
              value: hasAnyTarget ? bar : 0,
              minHeight: 7.h,
              backgroundColor: Colors.white.withValues(alpha: 0.22),
              valueColor: const AlwaysStoppedAnimation<Color>(Colors.white),
            ),
          ),
          SizedBox(height: 14.h),
          Row(
            children: [
              Expanded(
                child: _HeroStat(
                  label: 'TARGET',
                  value: '${_fmtCases(data.totalTargetQuantity)} CS',
                ),
              ),
              Expanded(
                child: _HeroStat(
                  label: 'SOLD',
                  value:
                      '${_fmtCases(data.totalSoldQuantity)} CS · ${_fmtPacks(data.totalSoldQuantityPacks)} PKT',
                ),
              ),
              Expanded(
                child: _HeroStat(
                  label: 'SALES VALUE',
                  value: 'Rs. ${_fmtAmount(data.totalSoldAmount)}',
                ),
              ),
            ],
          ),
          if (data.totalPendingQuantityPacks > 0 ||
              data.totalReturnQuantityPacks > 0 ||
              data.totalFreeIssueQuantityPacks > 0) ...[
            SizedBox(height: 12.h),
            Wrap(
              spacing: 6.w,
              runSpacing: 6.h,
              children: [
                if (data.totalPendingQuantityPacks > 0)
                  _HeroChip(
                      'PENDING ${_fmtPacks(data.totalPendingQuantityPacks)} PKT'),
                if (data.totalReturnQuantityPacks > 0)
                  _HeroChip(
                      'RETURNED ${_fmtPacks(data.totalReturnQuantityPacks)} PKT'),
                if (data.totalFreeIssueQuantityPacks > 0)
                  _HeroChip(
                      'FREE ISSUE ${_fmtPacks(data.totalFreeIssueQuantityPacks)} PKT'),
              ],
            ),
          ],
        ],
      ),
    );
  }
}

class _HeroStat extends StatelessWidget {
  final String label;
  final String value;
  const _HeroStat({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          label,
          style: GoogleFonts.barlowCondensed(
            fontSize: 9.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.5,
            color: Colors.white.withValues(alpha: 0.75),
          ),
        ),
        SizedBox(height: 2.h),
        FittedBox(
          fit: BoxFit.scaleDown,
          alignment: Alignment.centerLeft,
          child: Text(
            value,
            style: GoogleFonts.barlowCondensed(
              fontSize: 15.sp,
              fontWeight: FontWeight.w800,
              color: Colors.white,
            ),
          ),
        ),
      ],
    );
  }
}

class _HeroChip extends StatelessWidget {
  final String text;
  const _HeroChip(this.text);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 9.w, vertical: 4.h),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.18),
        borderRadius: BorderRadius.circular(99),
      ),
      child: Text(
        text,
        style: GoogleFonts.barlowCondensed(
          fontSize: 10.sp,
          fontWeight: FontWeight.w700,
          letterSpacing: 1.0,
          color: Colors.white,
        ),
      ),
    );
  }
}

// ── Item card ─────────────────────────────────────────────────────────────────

class _ItemCard extends StatelessWidget {
  final ItemAchievement item;
  const _ItemCard({required this.item});

  @override
  Widget build(BuildContext context) {
    final pct = item.achievementPercent;
    final accent = item.hasTarget ? _accentForPercent(pct) : AppColors.foregroundMuted;

    return Container(
      padding: EdgeInsets.all(14.r),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14.r),
        border: Border.all(color: accent.withValues(alpha: 0.20)),
        boxShadow: [
          BoxShadow(
            color: AppColors.foreground.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
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
                        fontSize: 15.sp,
                        fontWeight: FontWeight.w700,
                        height: 1.15,
                        color: AppColors.foreground,
                      ),
                    ),
                  ],
                ),
              ),
              SizedBox(width: 10.w),
              _PctBadge(
                text: item.hasTarget ? '${pct.toStringAsFixed(1)}%' : 'NO TARGET',
                color: accent,
              ),
            ],
          ),
          if (item.hasTarget) ...[
            SizedBox(height: 10.h),
            ClipRRect(
              borderRadius: BorderRadius.circular(99),
              child: LinearProgressIndicator(
                value: (pct / 100).clamp(0.0, 1.0),
                minHeight: 6.h,
                backgroundColor: accent.withValues(alpha: 0.14),
                valueColor: AlwaysStoppedAnimation<Color>(accent),
              ),
            ),
          ],
          SizedBox(height: 12.h),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (item.hasTarget)
                Expanded(
                  child: _Qty(
                    label: 'TARGET',
                    value: '${_fmtCases(item.targetQuantity)} CS',
                    color: AppColors.foreground,
                  ),
                ),
              Expanded(
                flex: 2,
                child: _Qty(
                  label: 'SOLD',
                  value:
                      '${_fmtCases(item.soldQuantity)} CS · ${_fmtPacks(item.soldQuantityPacks)} PKT',
                  color: accent == AppColors.foregroundMuted
                      ? AppColors.foreground
                      : accent,
                ),
              ),
              Expanded(
                flex: 2,
                child: _Qty(
                  label: 'VALUE',
                  value: 'Rs. ${_fmtAmount(item.soldAmount)}',
                  color: AppColors.amber,
                ),
              ),
            ],
          ),
          if (item.pendingQuantityPacks > 0 ||
              item.returnQuantityPacks > 0 ||
              item.freeIssueQuantityPacks > 0) ...[
            SizedBox(height: 10.h),
            Wrap(
              spacing: 6.w,
              runSpacing: 6.h,
              children: [
                if (item.pendingQuantityPacks > 0)
                  _Tag(
                    text:
                        '+${_fmtPacks(item.pendingQuantityPacks)} PKT PENDING APPROVAL',
                    color: _amberAccent,
                  ),
                if (item.returnQuantityPacks > 0)
                  _Tag(
                    text: '−${_fmtPacks(item.returnQuantityPacks)} PKT RETURNED',
                    color: const Color(0xFFDC2626),
                  ),
                if (item.freeIssueQuantityPacks > 0)
                  _Tag(
                    text: '${_fmtPacks(item.freeIssueQuantityPacks)} PKT FREE ISSUE',
                    color: AppColors.foregroundMuted,
                  ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}

class _PctBadge extends StatelessWidget {
  final String text;
  final Color color;
  const _PctBadge({required this.text, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 4.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(99),
        border: Border.all(color: color.withValues(alpha: 0.30)),
      ),
      child: Text(
        text,
        style: GoogleFonts.barlowCondensed(
          fontSize: 14.sp,
          fontWeight: FontWeight.w900,
          letterSpacing: 0.5,
          color: color,
        ),
      ),
    );
  }
}

class _Qty extends StatelessWidget {
  final String label;
  final String value;
  final Color color;
  const _Qty({required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
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
        SizedBox(height: 2.h),
        FittedBox(
          fit: BoxFit.scaleDown,
          alignment: Alignment.centerLeft,
          child: Text(
            value,
            style: GoogleFonts.barlowCondensed(
              fontSize: 14.sp,
              fontWeight: FontWeight.w800,
              height: 1.1,
              color: color,
            ),
          ),
        ),
      ],
    );
  }
}

class _Tag extends StatelessWidget {
  final String text;
  final Color color;
  const _Tag({required this.text, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(6.r),
      ),
      child: Text(
        text,
        style: GoogleFonts.barlowCondensed(
          fontSize: 10.sp,
          fontWeight: FontWeight.w700,
          letterSpacing: 0.6,
          color: color,
        ),
      ),
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
