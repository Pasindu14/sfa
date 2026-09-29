import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/supervisor_sales_summary/domain/entities/rep_billing_summary.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/cubit/supervisor_sales_summary_cubit.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/cubit/supervisor_sales_summary_state.dart';

// Every Icons.* here must already be used by release 1.0.8+10 — a Shorebird
// patch cannot ship new glyphs of the tree-shaken icon font.

const _months = [
  'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
];

String _fmtDate(DateTime d) => '${_months[d.month - 1]} ${d.day}, ${d.year}';

String _fmtRange(DateTimeRange r) {
  final a = r.start, b = r.end;
  if (DateUtils.isSameDay(a, b)) return _fmtDate(a);
  if (a.year == b.year) {
    return '${_months[a.month - 1]} ${a.day} – ${_months[b.month - 1]} ${b.day}, ${b.year}';
  }
  return '${_fmtDate(a)} – ${_fmtDate(b)}';
}

String _fmtLkr(double v) =>
    'LKR ${v.toStringAsFixed(2).replaceAllMapped(RegExp(r'(\d)(?=(\d{3})+\.)'), (m) => '${m[1]},')}';

class SupervisorSalesSummaryPage extends StatelessWidget {
  const SupervisorSalesSummaryPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF5F4EE),
      body: Column(
        children: [
          const _OrangeAppBar(),
          Expanded(
            child: BlocBuilder<SupervisorSalesSummaryCubit,
                SupervisorSalesSummaryState>(
              builder: (context, state) => switch (state) {
                SalesSummaryLoadingReps() => const Center(child: AppSpinner()),
                SalesSummaryRepsError(:final message) => _ErrorBody(
                    message: message,
                    onRetry: () =>
                        context.read<SupervisorSalesSummaryCubit>().loadReps(),
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

// ── App bar ───────────────────────────────────────────────────────────────────

class _OrangeAppBar extends StatelessWidget {
  const _OrangeAppBar();

  @override
  Widget build(BuildContext context) {
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: const SystemUiOverlayStyle(
        statusBarColor: Colors.transparent,
        statusBarIconBrightness: Brightness.light,
      ),
      child: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [AppColors.primaryDark, AppColors.primary],
          ),
        ),
        child: SafeArea(
          bottom: false,
          child: Stack(
            children: [
              Positioned(
                right: -18.w,
                top: -18.r,
                child: Container(
                  width: 90.r,
                  height: 90.r,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: Colors.white.withValues(alpha: 0.07),
                  ),
                ),
              ),
              Padding(
                padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 10.r),
                child: Row(
                  children: [
                    GestureDetector(
                      onTap: () => context.pop(),
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
                        Text('SALES SUMMARY',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 18.sp,
                              fontWeight: FontWeight.w800,
                              letterSpacing: 1.5,
                              height: 1.0,
                              color: Colors.white,
                            )),
                        SizedBox(height: 2.r),
                        Text('Rep totals over a date range',
                            style: GoogleFonts.barlow(
                              fontSize: 11.sp,
                              color: Colors.white.withValues(alpha: 0.70),
                            )),
                      ],
                    ),
                    const Spacer(),
                    Container(
                      width: 38.r,
                      height: 38.r,
                      margin: EdgeInsets.only(right: 16.w),
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(10.r),
                      ),
                      child: Icon(Icons.bar_chart_rounded,
                          size: 18.r, color: Colors.white),
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

// ── Error (reps failed) ───────────────────────────────────────────────────────

class _ErrorBody extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  const _ErrorBody({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.error_outline_rounded, size: 36.r, color: AppColors.error),
            SizedBox(height: 12.h),
            Text(message,
                textAlign: TextAlign.center,
                style: GoogleFonts.barlow(
                    fontSize: 14.sp, color: AppColors.foregroundMuted)),
            SizedBox(height: 16.h),
            FilledButton(
              style: FilledButton.styleFrom(backgroundColor: AppColors.primary),
              onPressed: onRetry,
              child: const Text('Try Again'),
            ),
          ],
        ),
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
        _StepCard(
          step: '01',
          label: 'SALES REP',
          icon: Icons.person_rounded,
          isComplete: state.selectedRep != null,
          child: _SelectBox(
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
                  borderRadius:
                      BorderRadius.vertical(top: Radius.circular(20.r)),
                ),
                builder: (_) => _RepSheet(
                    reps: state.reps, selected: state.selectedRep),
              );
              if (rep != null) cubit.selectRep(rep);
            },
          ),
        ),
        const _StepConnector(),
        _StepCard(
          step: '02',
          label: 'DATE RANGE',
          icon: Icons.date_range_rounded,
          isComplete: true,
          child: _RangePicker(
            range: state.range,
            enabled: !state.isLoading,
            onChanged: cubit.selectRange,
          ),
        ),
        SizedBox(height: 24.h),
        _GetSummaryButton(
          enabled: state.canLoad,
          loading: state.isLoading,
          onTap: cubit.load,
        ),
        if (state.error != null) ...[
          SizedBox(height: 16.h),
          _ErrorBanner(message: state.error!, onRetry: cubit.load),
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

// ── Step card ─────────────────────────────────────────────────────────────────

class _StepCard extends StatelessWidget {
  final String step;
  final String label;
  final IconData icon;
  final bool isComplete;
  final Widget child;

  const _StepCard({
    required this.step,
    required this.label,
    required this.icon,
    required this.isComplete,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF1A1A11).withValues(alpha: 0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 12.h),
            decoration: const BoxDecoration(
              border: Border(bottom: BorderSide(color: Color(0xFFEEEDE6))),
            ),
            child: Row(
              children: [
                Container(
                  width: 28.r,
                  height: 28.r,
                  decoration: BoxDecoration(
                    color: isComplete
                        ? AppColors.primary
                        : const Color(0xFFEEEDE6),
                    borderRadius: BorderRadius.circular(8.r),
                  ),
                  child: Center(
                    child: Text(step,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 11.sp,
                          fontWeight: FontWeight.w800,
                          color: isComplete
                              ? Colors.white
                              : AppColors.foregroundMuted,
                        )),
                  ),
                ),
                SizedBox(width: 10.w),
                Icon(icon,
                    size: 14.r,
                    color: isComplete
                        ? AppColors.primary
                        : AppColors.foregroundMuted),
                SizedBox(width: 6.w),
                Text(label,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 12.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 2.0,
                      color: isComplete
                          ? AppColors.foreground
                          : AppColors.foregroundMuted,
                    )),
                const Spacer(),
                if (isComplete)
                  Container(
                    padding:
                        EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
                    decoration: BoxDecoration(
                      color: AppColors.primary.withValues(alpha: 0.10),
                      borderRadius: BorderRadius.circular(20.r),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.check_rounded,
                            size: 10.r, color: AppColors.primary),
                        SizedBox(width: 3.w),
                        Text('SET',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 9.sp,
                              fontWeight: FontWeight.w700,
                              letterSpacing: 1.0,
                              color: AppColors.primary,
                            )),
                      ],
                    ),
                  ),
              ],
            ),
          ),
          Padding(padding: EdgeInsets.all(16.r), child: child),
        ],
      ),
    );
  }
}

class _StepConnector extends StatelessWidget {
  const _StepConnector();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(left: 28.w),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: List.generate(
          3,
          (_) => Container(
            width: 2.w,
            height: 5.h,
            margin: EdgeInsets.symmetric(vertical: 1.5.h),
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.25),
              borderRadius: BorderRadius.circular(1.r),
            ),
          ),
        ),
      ),
    );
  }
}

// ── Select box (rep) ──────────────────────────────────────────────────────────

class _SelectBox extends StatelessWidget {
  final IconData icon;
  final String text;
  final bool filled;
  final bool enabled;
  final VoidCallback onTap;

  const _SelectBox({
    required this.icon,
    required this.text,
    required this.filled,
    required this.enabled,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: enabled ? onTap : null,
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: 14.w, vertical: 13.h),
        decoration: BoxDecoration(
          color: filled
              ? AppColors.primary.withValues(alpha: 0.04)
              : const Color(0xFFF8F7F2),
          borderRadius: BorderRadius.circular(10.r),
          border: Border.all(
            color: filled
                ? AppColors.primary.withValues(alpha: 0.30)
                : const Color(0xFFE5E4DC),
          ),
        ),
        child: Row(
          children: [
            Icon(icon,
                size: 16.r,
                color: filled ? AppColors.primary : AppColors.foregroundMuted),
            SizedBox(width: 10.w),
            Expanded(
              child: Text(text,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.barlow(
                    fontSize: 14.sp,
                    fontWeight: filled ? FontWeight.w600 : FontWeight.w400,
                    color: filled
                        ? AppColors.foreground
                        : AppColors.foregroundMuted,
                  )),
            ),
            Icon(Icons.keyboard_arrow_down_rounded,
                size: 18.r, color: AppColors.foregroundMuted),
          ],
        ),
      ),
    );
  }
}

class _RepSheet extends StatefulWidget {
  final List<RepSummary> reps;
  final RepSummary? selected;
  const _RepSheet({required this.reps, required this.selected});

  @override
  State<_RepSheet> createState() => _RepSheetState();
}

class _RepSheetState extends State<_RepSheet> {
  String _q = '';

  @override
  Widget build(BuildContext context) {
    final list = widget.reps
        .where((r) => r.userName.toLowerCase().contains(_q.toLowerCase()))
        .toList();
    return FractionallySizedBox(
      heightFactor: 0.75,
      child: Padding(
        padding: EdgeInsets.only(
            bottom: MediaQuery.of(context).viewInsets.bottom),
        child: Column(
          children: [
            SizedBox(height: 10.h),
            Container(
              width: 40.w,
              height: 4.h,
              decoration: BoxDecoration(
                color: AppColors.surfaceVariant,
                borderRadius: BorderRadius.circular(2.r),
              ),
            ),
            Padding(
              padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 8.h),
              child: TextField(
                autofocus: false,
                onChanged: (v) => setState(() => _q = v),
                decoration: InputDecoration(
                  hintText: 'Search sales rep',
                  prefixIcon: Icon(Icons.search_rounded, size: 18.r),
                  isDense: true,
                  border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10.r)),
                ),
              ),
            ),
            Expanded(
              child: list.isEmpty
                  ? Center(
                      child: Text('No reps found',
                          style: GoogleFonts.barlow(
                              color: AppColors.foregroundMuted)))
                  : ListView.separated(
                      padding: EdgeInsets.symmetric(horizontal: 16.w),
                      itemCount: list.length,
                      separatorBuilder: (_, __) =>
                          Divider(height: 1, color: AppColors.surfaceVariant),
                      itemBuilder: (ctx, i) {
                        final r = list[i];
                        final sel = widget.selected?.userId == r.userId;
                        return ListTile(
                          contentPadding: EdgeInsets.zero,
                          onTap: () => Navigator.of(ctx).pop(r),
                          leading: Icon(Icons.person_rounded,
                              color: sel
                                  ? AppColors.primary
                                  : AppColors.foregroundMuted),
                          title: Text(r.userName,
                              style: GoogleFonts.barlow(
                                fontSize: 14.sp,
                                fontWeight:
                                    sel ? FontWeight.w700 : FontWeight.w500,
                              )),
                          trailing: sel
                              ? const Icon(Icons.check_circle_rounded,
                                  color: AppColors.primary)
                              : null,
                        );
                      },
                    ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Date range picker ─────────────────────────────────────────────────────────

class _RangePicker extends StatelessWidget {
  final DateTimeRange range;
  final bool enabled;
  final ValueChanged<DateTimeRange> onChanged;

  const _RangePicker({
    required this.range,
    required this.enabled,
    required this.onChanged,
  });

  static DateTime _day(DateTime d) => DateTime(d.year, d.month, d.day);

  List<(String, DateTimeRange)> _presets() {
    final today = _day(DateTime.now());
    final weekStart = today.subtract(Duration(days: today.weekday - 1));
    final monthStart = DateTime(today.year, today.month, 1);
    final lastMonthStart = DateTime(today.year, today.month - 1, 1);
    final lastMonthEnd = monthStart.subtract(const Duration(days: 1));
    return [
      ('Today', DateTimeRange(start: today, end: today)),
      ('This week', DateTimeRange(start: weekStart, end: today)),
      ('This month', DateTimeRange(start: monthStart, end: today)),
      ('Last month', DateTimeRange(start: lastMonthStart, end: lastMonthEnd)),
    ];
  }

  Future<void> _custom(BuildContext context) async {
    final today = _day(DateTime.now());
    final picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(today.year - 1, today.month, today.day),
      lastDate: today,
      initialDateRange: range,
      helpText: 'SELECT DATE RANGE (MAX 92 DAYS)',
      builder: (context, child) => Theme(
        data: Theme.of(context).copyWith(
          colorScheme: const ColorScheme.light(
            primary: AppColors.primary,
            onPrimary: Colors.white,
            surface: Colors.white,
          ),
        ),
        child: child!,
      ),
    );
    if (picked != null) {
      onChanged(DateTimeRange(start: _day(picked.start), end: _day(picked.end)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final presets = _presets();
    final matched = presets.where((p) => p.$2 == range).firstOrNull;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 8.w,
          runSpacing: 8.h,
          children: [
            for (final p in presets)
              _Chip(
                label: p.$1,
                selected: matched?.$1 == p.$1,
                onTap: enabled ? () => onChanged(p.$2) : null,
              ),
            _Chip(
              label: 'Custom…',
              icon: Icons.edit_calendar_rounded,
              selected: matched == null,
              onTap: enabled ? () => _custom(context) : null,
            ),
          ],
        ),
        SizedBox(height: 12.h),
        _SelectBox(
          icon: Icons.calendar_month_rounded,
          text: _fmtRange(range),
          filled: true,
          enabled: enabled,
          onTap: () => _custom(context),
        ),
      ],
    );
  }
}

class _Chip extends StatelessWidget {
  final String label;
  final IconData? icon;
  final bool selected;
  final VoidCallback? onTap;

  const _Chip({
    required this.label,
    required this.selected,
    required this.onTap,
    this.icon,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 7.h),
        decoration: BoxDecoration(
          color: selected ? AppColors.primary : Colors.white,
          borderRadius: BorderRadius.circular(20.r),
          border: Border.all(
            color: selected
                ? AppColors.primary
                : AppColors.primary.withValues(alpha: 0.25),
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (icon != null) ...[
              Icon(icon,
                  size: 12.r,
                  color: selected ? Colors.white : AppColors.primary),
              SizedBox(width: 4.w),
            ],
            Text(label,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 12.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.4,
                  color: selected ? Colors.white : AppColors.primary,
                )),
          ],
        ),
      ),
    );
  }
}

// ── Get summary button ────────────────────────────────────────────────────────

class _GetSummaryButton extends StatelessWidget {
  final bool enabled;
  final bool loading;
  final VoidCallback onTap;

  const _GetSummaryButton({
    required this.enabled,
    required this.loading,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final active = enabled || loading;
    return GestureDetector(
      onTap: enabled ? onTap : null,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        height: 54.h,
        decoration: BoxDecoration(
          gradient: active
              ? const LinearGradient(
                  colors: [AppColors.primaryDark, AppColors.primary])
              : null,
          color: active ? null : const Color(0xFFE5E4DC),
          borderRadius: BorderRadius.circular(14.r),
          boxShadow: active
              ? [
                  BoxShadow(
                    color: AppColors.primary.withValues(alpha: 0.30),
                    blurRadius: 16,
                    offset: const Offset(0, 6),
                  ),
                ]
              : null,
        ),
        child: Center(
          child: loading
              ? const AppSpinner(color: Colors.white)
              : Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.bar_chart_rounded,
                        size: 18.r,
                        color: active
                            ? Colors.white
                            : AppColors.foregroundMuted),
                    SizedBox(width: 8.w),
                    Text('GET SUMMARY',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 16.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.5,
                          color: active
                              ? Colors.white
                              : AppColors.foregroundMuted,
                        )),
                  ],
                ),
        ),
      ),
    );
  }
}

class _ErrorBanner extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  const _ErrorBanner({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(12.r),
      decoration: BoxDecoration(
        color: AppColors.error.withValues(alpha: 0.06),
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.error.withValues(alpha: 0.2)),
      ),
      child: Row(
        children: [
          Icon(Icons.error_outline_rounded, size: 18.r, color: AppColors.error),
          SizedBox(width: 10.w),
          Expanded(
            child: Text(message,
                style: GoogleFonts.barlow(
                    fontSize: 12.sp, color: AppColors.foreground)),
          ),
          TextButton(onPressed: onRetry, child: const Text('Retry')),
        ],
      ),
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
        _HeroCard(summary: s, repName: repName, rangeLabel: _fmtRange(range)),
        SizedBox(height: 12.h),
        Row(
          children: [
            Expanded(
              child: _StatTile(
                icon: Icons.check_circle_rounded,
                label: 'SALES · APPROVED',
                value: _fmtLkr(s.approvedSales),
                caption: '${s.approvedCount} bill${s.approvedCount == 1 ? '' : 's'}',
                color: AppColors.primary,
              ),
            ),
            SizedBox(width: 10.w),
            Expanded(
              child: _StatTile(
                icon: Icons.hourglass_top_rounded,
                label: 'PENDING APPROVAL',
                value: _fmtLkr(s.pendingValue),
                caption: '${s.pendingCount} bill${s.pendingCount == 1 ? '' : 's'}',
                color: AppColors.primary,
              ),
            ),
          ],
        ),
        SizedBox(height: 10.h),
        _StatTile(
          icon: Icons.local_offer_rounded,
          label: 'TOTAL DISCOUNT',
          value: _fmtLkr(s.totalDiscount),
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
                value: _fmtLkr(s.goodReturn),
                caption: 'Resellable stock',
                color: AppColors.primary,
              ),
            ),
            SizedBox(width: 10.w),
            Expanded(
              child: _StatTile(
                icon: Icons.warning_amber_rounded,
                label: 'MARKET RETURN',
                value: _fmtLkr(s.marketReturn),
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
              fontSize: 10.sp, color: AppColors.foregroundMuted),
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
                child: Text(label,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 12.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 1.0,
                      color: AppColors.foregroundMuted,
                    )),
              ),
              Text('${minus ? '−' : ''}$value',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 15.sp,
                    fontWeight: FontWeight.w700,
                    color: AppColors.foreground,
                  )),
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
              Icon(Icons.receipt_long_rounded,
                  size: 15.r, color: AppColors.primary),
              SizedBox(width: 6.w),
              Text('NET SALES',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 2.0,
                    color: AppColors.primary,
                  )),
              const Spacer(),
              Text('Approved + pending',
                  style: GoogleFonts.barlow(
                      fontSize: 10.sp, color: AppColors.foregroundMuted)),
            ],
          ),
          SizedBox(height: 6.h),
          row('SALES', _fmtLkr(s.grossSales)),
          row('DISCOUNT', _fmtLkr(s.totalDiscount), minus: true),
          row('GOOD RETURN', _fmtLkr(s.goodReturn), minus: true),
          row('MARKET RETURN', _fmtLkr(s.marketReturn), minus: true),
          Divider(height: 14.h, color: AppColors.surfaceVariant),
          Row(
            children: [
              Expanded(
                child: Text('NET SALES',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 14.sp,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 1.2,
                      color: AppColors.foreground,
                    )),
              ),
              Text(_fmtLkr(s.netSales),
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 22.sp,
                    fontWeight: FontWeight.w900,
                    letterSpacing: -0.3,
                    color: AppColors.primaryDark,
                  )),
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
                    padding:
                        EdgeInsets.symmetric(horizontal: 10.w, vertical: 4.h),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.18),
                      borderRadius: BorderRadius.circular(20.r),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.date_range_rounded,
                            size: 11.r, color: Colors.white),
                        SizedBox(width: 5.w),
                        Text(rangeLabel.toUpperCase(),
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 10.sp,
                              fontWeight: FontWeight.w700,
                              letterSpacing: 1.2,
                              color: Colors.white,
                            )),
                      ],
                    ),
                  ),
                ],
              ),
              SizedBox(height: 12.h),
              Text(repName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.barlow(
                    fontSize: 13.sp,
                    color: Colors.white.withValues(alpha: 0.8),
                  )),
              SizedBox(height: 2.h),
              Text('TOTAL BILLED',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 11.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 2.0,
                    color: Colors.white.withValues(alpha: 0.75),
                  )),
              FittedBox(
                fit: BoxFit.scaleDown,
                alignment: Alignment.centerLeft,
                child: Text(_fmtLkr(s.totalBilled),
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 34.sp,
                      fontWeight: FontWeight.w900,
                      height: 1.05,
                      letterSpacing: -0.5,
                      color: Colors.white,
                    )),
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
                    Icon(Icons.receipt_long_rounded,
                        size: 16.r, color: Colors.white),
                    SizedBox(width: 8.w),
                    Text('${s.totalBills}',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 20.sp,
                          fontWeight: FontWeight.w900,
                          height: 1.0,
                          color: Colors.white,
                        )),
                    SizedBox(width: 6.w),
                    Text('BILLS',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 10.sp,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 1.5,
                          color: Colors.white.withValues(alpha: 0.8),
                        )),
                    SizedBox(width: 10.w),
                    Expanded(
                      child: Text(counts,
                          maxLines: 2,
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 10.sp,
                            fontWeight: FontWeight.w600,
                            color: Colors.white.withValues(alpha: 0.85),
                          )),
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
        Text(label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.barlowCondensed(
              fontSize: 10.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.2,
              color: Colors.white.withValues(alpha: 0.85),
            )),
        SizedBox(height: 2.h),
        FittedBox(
          fit: BoxFit.scaleDown,
          alignment: Alignment.centerLeft,
          child: Text(value,
              style: GoogleFonts.barlowCondensed(
                fontSize: 19.sp,
                fontWeight: FontWeight.w900,
                height: 1.1,
                color: Colors.white,
              )),
        ),
        Text(caption,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.barlow(
              fontSize: 9.sp,
              color: Colors.white.withValues(alpha: 0.75),
            )),
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
