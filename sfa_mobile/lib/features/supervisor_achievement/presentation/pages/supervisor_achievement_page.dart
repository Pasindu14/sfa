import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/supervisor_achievement/presentation/cubit/supervisor_achievement_cubit.dart';
import 'package:uswatte/features/supervisor_achievement/presentation/cubit/supervisor_achievement_state.dart';

const _greenAccent = Color(0xFF22C55E);
const _amberAccent = Color(0xFFF59E0B);

Color _accentForPercent(double pct) {
  if (pct >= 100) return _greenAccent;
  if (pct >= 75) return _amberAccent;
  return AppColors.primary;
}

const _monthNames = [
  'JANUARY', 'FEBRUARY', 'MARCH', 'APRIL', 'MAY', 'JUNE',
  'JULY', 'AUGUST', 'SEPTEMBER', 'OCTOBER', 'NOVEMBER', 'DECEMBER',
];

class SupervisorAchievementPage extends StatelessWidget {
  const SupervisorAchievementPage({super.key});

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: const Color(0xFFF5F4EE),
      body: Column(
        children: [
          _OrangeAppBar(),
          Expanded(
            child: BlocBuilder<SupervisorAchievementCubit, SupervisorAchievementState>(
              builder: (context, state) => switch (state) {
                SupervisorAchievementLoadingReps() => const _LoadingReps(),
                SupervisorAchievementRepsError(:final message) =>
                  _RepsError(message: message),
                SupervisorAchievementReady() => _ReadyBody(state: state),
              },
            ),
          ),
        ],
      ),
    );
  }
}

// ── App bar ────────────────────────────────────────────────────────────────────

class _OrangeAppBar extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
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
                  Expanded(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'REP ACHIEVEMENT',
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
                          'Item-wise target vs sold',
                          style: GoogleFonts.barlow(
                            fontSize: 11.sp,
                            color: Colors.white.withValues(alpha: 0.70),
                          ),
                        ),
                      ],
                    ),
                  ),
                  Container(
                    width: 38.r,
                    height: 38.r,
                    margin: EdgeInsets.only(right: 16.w),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(10.r),
                    ),
                    child: Icon(Icons.emoji_events_rounded,
                        size: 18.r, color: Colors.white),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Loading / error states ─────────────────────────────────────────────────────

class _LoadingReps extends StatelessWidget {
  const _LoadingReps();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const AppSpinner(),
        ],
      ),
    );
  }
}

class _RepsError extends StatelessWidget {
  final String message;
  const _RepsError({required this.message});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 60.r,
              height: 60.r,
              decoration: BoxDecoration(
                color: AppColors.error.withValues(alpha: 0.08),
                shape: BoxShape.circle,
              ),
              child: Icon(Icons.error_outline_rounded,
                  size: 28.r, color: AppColors.error),
            ),
            SizedBox(height: 14.h),
            Text(message,
                textAlign: TextAlign.center,
                style: GoogleFonts.barlow(
                    fontSize: 14.sp,
                    color: AppColors.foregroundMuted,
                    height: 1.5)),
            SizedBox(height: 20.h),
            GestureDetector(
              onTap: () =>
                  context.read<SupervisorAchievementCubit>().loadReps(),
              child: Container(
                padding:
                    EdgeInsets.symmetric(horizontal: 24.w, vertical: 12.h),
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  borderRadius: BorderRadius.circular(10.r),
                ),
                child: Text('Try Again',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 15.sp,
                      fontWeight: FontWeight.w700,
                      color: Colors.white,
                      letterSpacing: 0.5,
                    )),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Ready body ─────────────────────────────────────────────────────────────────

class _ReadyBody extends StatelessWidget {
  final SupervisorAchievementReady state;
  const _ReadyBody({required this.state});

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      color: AppColors.primary,
      onRefresh: () => context.read<SupervisorAchievementCubit>().refresh(),
      child: CustomScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        slivers: [
          SliverToBoxAdapter(
            child: Padding(
              padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 0),
              child: Column(
                children: [
                  // ── Step 01: Sales Rep ─────────────────────────────────────
                  _StepCard(
                    step: '01',
                    label: 'SALES REP',
                    icon: Icons.person_rounded,
                    isComplete: state.selectedRep != null,
                    child: _RepSelectField(
                      value: state.selectedRep,
                      reps: state.reps,
                      enabled: !state.isLoading,
                      onChanged: (rep) {
                        if (rep != null) {
                          context
                              .read<SupervisorAchievementCubit>()
                              .selectRep(rep);
                        }
                      },
                    ),
                  ),
                  _StepConnector(),

                  // ── Step 02: Month / Year ──────────────────────────────────
                  _StepCard(
                    step: '02',
                    label: 'MONTH & YEAR',
                    icon: Icons.calendar_month_rounded,
                    isComplete: true,
                    child: _MonthYearField(
                      month: state.month,
                      year: state.year,
                      enabled: !state.isLoading,
                      onChanged: (y, m) {
                        context
                            .read<SupervisorAchievementCubit>()
                            .changeMonth(y, m);
                      },
                    ),
                  ),
                ],
              ),
            ),
          ),

          // ── Results ──────────────────────────────────────────────────────
          if (state.selectedRep == null)
            SliverToBoxAdapter(child: _NoRepPrompt())
          else if (state.isLoading)
            const SliverToBoxAdapter(child: _DataLoading())
          else if (state.dataError != null)
            SliverToBoxAdapter(
              child: _DataError(
                message: state.dataError!,
                onRetry: () =>
                    context.read<SupervisorAchievementCubit>().refresh(),
              ),
            )
          else if (state.data != null) ...[
            // ── Value achievement (ring + KPI) ────────────────────────────
            SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 0),
                child: _ValueAchievementSection(
                  totalSales: state.totalSales ?? 0,
                  totalTarget: state.totalTarget ?? 0,
                  repName: state.selectedRep!.userName,
                  month: state.month,
                  year: state.year,
                ),
              ),
            ),
            // ── Item-wise summary strip ───────────────────────────────────
            SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.only(top: 16.h),
                child: _SummaryStrip(data: state.data!),
              ),
            ),
            if (state.data!.items.isEmpty)
              SliverToBoxAdapter(
                child: Padding(
                  padding: EdgeInsets.only(top: 60.h),
                  child: const _EmptyView(),
                ),
              )
            else
              SliverPadding(
                padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 40.h),
                sliver: SliverList.separated(
                  itemCount: state.data!.items.length,
                  separatorBuilder: (_, __) => SizedBox(height: 10.h),
                  itemBuilder: (_, i) =>
                      _ItemRow(item: state.data!.items[i]),
                ),
              ),
          ],
        ],
      ),
    );
  }
}

// ── Step card + connector ─────────────────────────────────────────────────────

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
    final accent = isComplete ? AppColors.primary : AppColors.foregroundMuted;
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14.r),
        border: Border.all(
          color: isComplete
              ? AppColors.primary.withValues(alpha: 0.20)
              : AppColors.surfaceVariant,
        ),
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
          Padding(
            padding: EdgeInsets.fromLTRB(14.w, 12.h, 14.w, 8.h),
            child: Row(
              children: [
                Container(
                  width: 26.r,
                  height: 26.r,
                  decoration: BoxDecoration(
                    color: accent.withValues(alpha: 0.10),
                    borderRadius: BorderRadius.circular(7.r),
                  ),
                  child: Center(
                    child: Text(
                      step,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 12.sp,
                        fontWeight: FontWeight.w900,
                        color: accent,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ),
                ),
                SizedBox(width: 8.w),
                Icon(icon, size: 14.r, color: accent),
                SizedBox(width: 4.w),
                Text(
                  label,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 11.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 1.5,
                    color: accent,
                  ),
                ),
                const Spacer(),
                if (isComplete)
                  Icon(Icons.check_circle_rounded,
                      size: 16.r,
                      color: AppColors.primary.withValues(alpha: 0.60)),
              ],
            ),
          ),
          Divider(
              height: 1,
              thickness: 1,
              color: AppColors.surfaceVariant.withValues(alpha: 0.7)),
          Padding(
            padding: EdgeInsets.fromLTRB(14.w, 10.h, 14.w, 14.h),
            child: child,
          ),
        ],
      ),
    );
  }
}

class _StepConnector extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(left: 26.w),
      child: Container(
        width: 2,
        height: 16.h,
        decoration: BoxDecoration(
          color: AppColors.surfaceVariant,
          borderRadius: BorderRadius.circular(1.r),
        ),
      ),
    );
  }
}

// ── Rep select field ───────────────────────────────────────────────────────────

class _RepSelectField extends StatelessWidget {
  final RepSummary? value;
  final List<RepSummary> reps;
  final bool enabled;
  final ValueChanged<RepSummary?> onChanged;

  const _RepSelectField({
    required this.value,
    required this.reps,
    required this.enabled,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: enabled
          ? () => _showRepSheet(context)
          : null,
      child: Container(
        height: 50.h,
        decoration: BoxDecoration(
          color: enabled ? AppColors.surface : AppColors.surface.withValues(alpha: 0.5),
          borderRadius: BorderRadius.circular(10.r),
          border: Border.all(
            color: value != null
                ? AppColors.primary.withValues(alpha: 0.35)
                : AppColors.surfaceVariant,
          ),
        ),
        padding: EdgeInsets.symmetric(horizontal: 12.w),
        child: Row(
          children: [
            Icon(
              value != null
                  ? Icons.check_circle_outline_rounded
                  : Icons.search_rounded,
              size: 18.r,
              color: value != null
                  ? AppColors.primary
                  : AppColors.foregroundMuted,
            ),
            SizedBox(width: 10.w),
            Expanded(
              child: Text(
                value?.userName ?? 'Select a sales rep...',
                style: GoogleFonts.barlow(
                  fontSize: 14.sp,
                  color: value != null
                      ? AppColors.foreground
                      : AppColors.foregroundMuted,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ),
            Icon(Icons.keyboard_arrow_down_rounded,
                size: 20.r, color: AppColors.foregroundMuted),
          ],
        ),
      ),
    );
  }

  Future<void> _showRepSheet(BuildContext context) async {
    final cubit = context.read<SupervisorAchievementCubit>();
    final selected = await showModalBottomSheet<RepSummary>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _RepPickerSheet(
        reps: reps,
        selected: value,
      ),
    );
    if (selected != null) {
      cubit.selectRep(selected);
    }
  }
}

class _RepPickerSheet extends StatefulWidget {
  final List<RepSummary> reps;
  final RepSummary? selected;
  const _RepPickerSheet({required this.reps, this.selected});

  @override
  State<_RepPickerSheet> createState() => _RepPickerSheetState();
}

class _RepPickerSheetState extends State<_RepPickerSheet> {
  final _searchCtrl = TextEditingController();
  List<RepSummary> _filtered = [];

  @override
  void initState() {
    super.initState();
    _filtered = widget.reps;
    _searchCtrl.addListener(() {
      final q = _searchCtrl.text.toLowerCase();
      setState(() {
        _filtered = q.isEmpty
            ? widget.reps
            : widget.reps
                .where((r) => r.userName.toLowerCase().contains(q))
                .toList();
      });
    });
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.65,
      minChildSize: 0.40,
      maxChildSize: 0.92,
      builder: (_, scrollCtrl) => Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
        ),
        child: Column(
          children: [
            // Header
            Container(
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [AppColors.primaryDark, AppColors.primary],
                ),
                borderRadius:
                    BorderRadius.vertical(top: Radius.circular(20.r)),
              ),
              padding: EdgeInsets.fromLTRB(20.w, 16.h, 16.w, 16.h),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'SELECT SALES REP',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 16.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 1.5,
                            color: Colors.white,
                          ),
                        ),
                        Text(
                          '${widget.reps.length} available',
                          style: GoogleFonts.barlow(
                            fontSize: 11.sp,
                            color: Colors.white.withValues(alpha: 0.75),
                          ),
                        ),
                      ],
                    ),
                  ),
                  GestureDetector(
                    onTap: () => Navigator.of(context).pop(),
                    child: Container(
                      width: 34.r,
                      height: 34.r,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.20),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(Icons.close_rounded,
                          size: 16.r, color: Colors.white),
                    ),
                  ),
                ],
              ),
            ),

            // Search bar
            Container(
              height: 52.h,
              color: Colors.white,
              padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
              child: TextField(
                controller: _searchCtrl,
                style: GoogleFonts.barlow(fontSize: 14.sp),
                decoration: InputDecoration(
                  hintText: 'Search...',
                  hintStyle: GoogleFonts.barlow(
                      fontSize: 13.sp, color: AppColors.foregroundMuted),
                  prefixIcon: Icon(Icons.search_rounded,
                      size: 18.r, color: AppColors.foregroundMuted),
                  suffixIcon: _searchCtrl.text.isNotEmpty
                      ? GestureDetector(
                          onTap: () => _searchCtrl.clear(),
                          child: Icon(Icons.close_rounded,
                              size: 16.r, color: AppColors.foregroundMuted),
                        )
                      : null,
                  filled: true,
                  fillColor: AppColors.surface,
                  contentPadding: EdgeInsets.zero,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10.r),
                    borderSide: BorderSide.none,
                  ),
                ),
              ),
            ),
            Divider(
                height: 1,
                thickness: 1,
                color: AppColors.surfaceVariant.withValues(alpha: 0.6)),

            // List
            Expanded(
              child: _filtered.isEmpty
                  ? Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.search_off_rounded,
                              size: 32.r,
                              color: AppColors.surfaceVariant),
                          SizedBox(height: 8.h),
                          Text('No results',
                              style: GoogleFonts.barlow(
                                  fontSize: 13.sp,
                                  color: AppColors.foregroundMuted)),
                        ],
                      ),
                    )
                  : ListView.builder(
                      controller: scrollCtrl,
                      padding:
                          EdgeInsets.symmetric(vertical: 8.h),
                      itemCount: _filtered.length,
                      itemBuilder: (_, i) {
                        final rep = _filtered[i];
                        final isSelected =
                            widget.selected?.userId == rep.userId;
                        return ListTile(
                          leading: Container(
                            width: 38.r,
                            height: 38.r,
                            decoration: BoxDecoration(
                              color: isSelected
                                  ? AppColors.primary
                                  : AppColors.surface,
                              shape: BoxShape.circle,
                            ),
                            child: Center(
                              child: Text(
                                rep.userName.isNotEmpty
                                    ? rep.userName[0].toUpperCase()
                                    : '?',
                                style: GoogleFonts.barlowCondensed(
                                  fontSize: 15.sp,
                                  fontWeight: FontWeight.w800,
                                  color: isSelected
                                      ? Colors.white
                                      : AppColors.foregroundMuted,
                                ),
                              ),
                            ),
                          ),
                          title: Text(
                            rep.userName,
                            style: GoogleFonts.barlow(
                              fontSize: 14.sp,
                              fontWeight: isSelected
                                  ? FontWeight.w600
                                  : FontWeight.w400,
                              color: AppColors.foreground,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                          trailing: isSelected
                              ? Icon(Icons.check_rounded,
                                  size: 18.r, color: AppColors.primary)
                              : null,
                          onTap: () => Navigator.of(context).pop(rep),
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

// ── Month/Year field ──────────────────────────────────────────────────────────

class _MonthYearField extends StatelessWidget {
  final int month;
  final int year;
  final bool enabled;
  final void Function(int year, int month) onChanged;

  const _MonthYearField({
    required this.month,
    required this.year,
    required this.enabled,
    required this.onChanged,
  });

  void _prev() {
    int m = month - 1;
    int y = year;
    if (m < 1) {
      m = 12;
      y -= 1;
    }
    onChanged(y, m);
  }

  void _next() {
    final now = DateTime.now();
    if (year >= now.year && month >= now.month) return;
    int m = month + 1;
    int y = year;
    if (m > 12) {
      m = 1;
      y += 1;
    }
    onChanged(y, m);
  }

  bool get _canGoNext {
    final now = DateTime.now();
    return !(year >= now.year && month >= now.month);
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: enabled ? () => _showMonthPicker(context) : null,
      child: Container(
        height: 58.h,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14.r),
          border: Border.all(
              color: AppColors.primary.withValues(alpha: 0.22), width: 1.5),
          boxShadow: [
            BoxShadow(
              color: AppColors.primary.withValues(alpha: 0.08),
              blurRadius: 12,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Row(
          children: [
            // Left arrow
            GestureDetector(
              onTap: enabled ? _prev : null,
              behavior: HitTestBehavior.opaque,
              child: SizedBox(
                width: 52.w,
                child: Icon(
                  Icons.chevron_left_rounded,
                  size: 24.r,
                  color: enabled
                      ? AppColors.primary
                      : AppColors.foregroundMuted.withValues(alpha: 0.3),
                ),
              ),
            ),
            // Month + year label
            Expanded(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    _monthNames[month - 1],
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 20.sp,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 2.0,
                      color: AppColors.primary,
                      height: 1.0,
                    ),
                  ),
                  SizedBox(height: 1.h),
                  Text(
                    '$year',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 11.sp,
                      fontWeight: FontWeight.w600,
                      letterSpacing: 1.5,
                      color: AppColors.foregroundMuted,
                      height: 1.0,
                    ),
                  ),
                ],
              ),
            ),
            // Right arrow
            GestureDetector(
              onTap: (enabled && _canGoNext) ? _next : null,
              behavior: HitTestBehavior.opaque,
              child: SizedBox(
                width: 52.w,
                child: Icon(
                  Icons.chevron_right_rounded,
                  size: 24.r,
                  color: (enabled && _canGoNext)
                      ? AppColors.primary
                      : AppColors.foregroundMuted.withValues(alpha: 0.3),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _showMonthPicker(BuildContext context) async {
    final result = await showModalBottomSheet<(int, int)>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _MonthYearPickerSheet(month: month, year: year),
    );
    if (result != null) {
      onChanged(result.$1, result.$2);
    }
  }
}


class _MonthYearPickerSheet extends StatefulWidget {
  final int month;
  final int year;
  const _MonthYearPickerSheet({required this.month, required this.year});

  @override
  State<_MonthYearPickerSheet> createState() => _MonthYearPickerSheetState();
}

class _MonthYearPickerSheetState extends State<_MonthYearPickerSheet> {
  late int _year;
  late int _month;

  @override
  void initState() {
    super.initState();
    _year = widget.year;
    _month = widget.month;
  }

  bool _isDisabled(int m) {
    final now = DateTime.now();
    return _year > now.year || (_year == now.year && m > now.month);
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
      ),
      padding: EdgeInsets.fromLTRB(20.w, 20.h, 20.w, 32.h),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Year row
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              GestureDetector(
                onTap: () => setState(() => _year -= 1),
                child: Container(
                  width: 36.r,
                  height: 36.r,
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(8.r),
                    border: Border.all(color: AppColors.surfaceVariant),
                  ),
                  child: Icon(Icons.remove_rounded,
                      size: 18.r, color: AppColors.foreground),
                ),
              ),
              SizedBox(width: 20.w),
              Text(
                '$_year',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 26.sp,
                  fontWeight: FontWeight.w900,
                  color: AppColors.foreground,
                  letterSpacing: -0.5,
                ),
              ),
              SizedBox(width: 20.w),
              GestureDetector(
                onTap: () {
                  final now = DateTime.now();
                  if (_year < now.year) setState(() => _year += 1);
                },
                child: Container(
                  width: 36.r,
                  height: 36.r,
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(8.r),
                    border: Border.all(color: AppColors.surfaceVariant),
                  ),
                  child: Icon(Icons.add_rounded,
                      size: 18.r, color: AppColors.foreground),
                ),
              ),
            ],
          ),
          SizedBox(height: 20.h),

          // Month grid
          GridView.count(
            crossAxisCount: 3,
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            mainAxisSpacing: 10.h,
            crossAxisSpacing: 10.w,
            childAspectRatio: 2.2,
            children: List.generate(12, (i) {
              final m = i + 1;
              final isSelected = m == _month;
              final disabled = _isDisabled(m);
              return GestureDetector(
                onTap: disabled
                    ? null
                    : () => setState(() => _month = m),
                child: Container(
                  decoration: BoxDecoration(
                    color: isSelected
                        ? AppColors.primary
                        : disabled
                            ? AppColors.surfaceVariant.withValues(alpha: 0.4)
                            : AppColors.surface,
                    borderRadius: BorderRadius.circular(8.r),
                    border: Border.all(
                      color: isSelected
                          ? AppColors.primary
                          : AppColors.surfaceVariant,
                    ),
                  ),
                  child: Center(
                    child: Text(
                      _monthNames[i].substring(0, 3),
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 13.sp,
                        fontWeight: FontWeight.w700,
                        letterSpacing: 0.5,
                        color: isSelected
                            ? Colors.white
                            : disabled
                                ? AppColors.foregroundMuted.withValues(alpha: 0.4)
                                : AppColors.foreground,
                      ),
                    ),
                  ),
                ),
              );
            }),
          ),
          SizedBox(height: 20.h),

          // Confirm button
          SizedBox(
            width: double.infinity,
            height: 48.h,
            child: ElevatedButton(
              onPressed: () =>
                  Navigator.of(context).pop((_year, _month)),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12.r)),
                elevation: 0,
              ),
              child: Text(
                'APPLY',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 15.sp,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 2.0,
                  color: Colors.white,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Prompts / data states ─────────────────────────────────────────────────────

class _NoRepPrompt extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(32.w, 48.h, 32.w, 0),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.person_search_rounded,
              size: 48.r, color: AppColors.surfaceVariant),
          SizedBox(height: 14.h),
          Text(
            'Select a rep to continue',
            style: GoogleFonts.barlowCondensed(
              fontSize: 18.sp,
              fontWeight: FontWeight.w700,
              color: AppColors.foreground,
            ),
          ),
          SizedBox(height: 6.h),
          Text(
            'Choose a sales rep above to view their item-wise achievement for the selected month.',
            textAlign: TextAlign.center,
            style: GoogleFonts.barlow(
                fontSize: 13.sp, color: AppColors.foregroundMuted),
          ),
        ],
      ),
    );
  }
}

class _DataLoading extends StatelessWidget {
  const _DataLoading();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(top: 60.h),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const AppSpinner(),
        ],
      ),
    );
  }
}

class _DataError extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  const _DataError({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(32.w, 48.h, 32.w, 0),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.cloud_off_rounded,
              size: 40.r, color: AppColors.foregroundMuted),
          SizedBox(height: 14.h),
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
    );
  }
}

// ── Value achievement hero card ────────────────────────────────────────────────

class _ValueAchievementSection extends StatelessWidget {
  final double totalSales;
  final double totalTarget;
  final String repName;
  final int month;
  final int year;

  const _ValueAchievementSection({
    required this.totalSales,
    required this.totalTarget,
    required this.repName,
    required this.month,
    required this.year,
  });

  @override
  Widget build(BuildContext context) {
    final pct = totalTarget > 0
        ? (totalSales / totalTarget * 100).clamp(0.0, 999.0)
        : 0.0;
    final accent = pct >= 100
        ? _greenAccent
        : pct >= 75
            ? _amberAccent
            : AppColors.primary;
    final darkAccent = pct >= 100
        ? const Color(0xFF15803D)
        : pct >= 75
            ? const Color(0xFFB45309)
            : AppColors.primaryDark;
    final ringValue = (pct / 100).clamp(0.0, 1.0);

    // Daily pace calculations
    final daysInMonth = DateTime(year, month + 1, 0).day;
    final now = DateTime.now();
    final isCurrentMonth = year == now.year && month == now.month;
    final daysElapsed = isCurrentMonth
        ? now.day.clamp(1, daysInMonth)
        : daysInMonth;
    final dailyTarget = totalTarget > 0 ? totalTarget / daysInMonth : 0.0;
    final dailySales = daysElapsed > 0 ? totalSales / daysElapsed : 0.0;
    final dailyPct = dailyTarget > 0
        ? (dailySales / dailyTarget * 100).clamp(0.0, 999.0)
        : 0.0;
    final monthLabel = '${_monthNames[month - 1]} $year';

    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [darkAccent, accent],
        ),
        borderRadius: BorderRadius.circular(20.r),
        boxShadow: [
          BoxShadow(
            color: accent.withValues(alpha: 0.38),
            blurRadius: 24,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(20.r),
        child: Stack(
          children: [
            // Decorative background circles
            Positioned(
              right: -28.r,
              top: -28.r,
              child: Container(
                width: 120.r,
                height: 120.r,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: Colors.white.withValues(alpha: 0.07),
                ),
              ),
            ),
            Positioned(
              left: -22.r,
              bottom: -22.r,
              child: Container(
                width: 90.r,
                height: 90.r,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: Colors.white.withValues(alpha: 0.05),
                ),
              ),
            ),

            // Content
            Padding(
              padding: EdgeInsets.fromLTRB(20.w, 18.h, 20.w, 18.h),
              child: Column(
                children: [
                  // Rep + month context pill
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.person_rounded,
                          size: 11.r,
                          color: Colors.white.withValues(alpha: 0.7)),
                      SizedBox(width: 4.w),
                      Text(
                        repName,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 12.sp,
                          fontWeight: FontWeight.w600,
                          letterSpacing: 1.0,
                          color: Colors.white.withValues(alpha: 0.85),
                        ),
                      ),
                      Container(
                        width: 3.r,
                        height: 3.r,
                        margin: EdgeInsets.symmetric(horizontal: 8.w),
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: Colors.white.withValues(alpha: 0.40),
                        ),
                      ),
                      Icon(Icons.calendar_today_rounded,
                          size: 10.r,
                          color: Colors.white.withValues(alpha: 0.7)),
                      SizedBox(width: 4.w),
                      Text(
                        monthLabel,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 12.sp,
                          fontWeight: FontWeight.w600,
                          letterSpacing: 1.0,
                          color: Colors.white.withValues(alpha: 0.85),
                        ),
                      ),
                    ],
                  ),
                  SizedBox(height: 20.h),

                  // Large achievement ring
                  Stack(
                    alignment: Alignment.center,
                    children: [
                      SizedBox(
                        width: 110.r,
                        height: 110.r,
                        child: CircularProgressIndicator(
                          value: ringValue,
                          strokeWidth: 8.r,
                          backgroundColor:
                              Colors.white.withValues(alpha: 0.18),
                          valueColor: const AlwaysStoppedAnimation<Color>(
                              Colors.white),
                          strokeCap: StrokeCap.round,
                        ),
                      ),
                      Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            '${pct.toStringAsFixed(0)}%',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 36.sp,
                              fontWeight: FontWeight.w900,
                              height: 1.0,
                              letterSpacing: -1.0,
                              color: Colors.white,
                            ),
                          ),
                          Text(
                            'ACHIEVED',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 9.sp,
                              fontWeight: FontWeight.w700,
                              letterSpacing: 2.0,
                              color: Colors.white.withValues(alpha: 0.70),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),

                  SizedBox(height: 20.h),
                  Container(height: 1, color: Colors.white.withValues(alpha: 0.20)),
                  SizedBox(height: 16.h),

                  // MTD Sales | TARGET stat row
                  Row(
                    children: [
                      Expanded(
                        child: Column(
                          children: [
                            Text(
                              'MTD SALES',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 9.sp,
                                fontWeight: FontWeight.w700,
                                letterSpacing: 1.5,
                                color: Colors.white.withValues(alpha: 0.65),
                              ),
                            ),
                            SizedBox(height: 3.h),
                            Text(
                              _fmtCurrency(totalSales),
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 22.sp,
                                fontWeight: FontWeight.w900,
                                height: 1.0,
                                letterSpacing: -0.5,
                                color: Colors.white,
                              ),
                            ),
                            Text(
                              'LKR',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 9.sp,
                                fontWeight: FontWeight.w600,
                                letterSpacing: 1.0,
                                color: Colors.white.withValues(alpha: 0.55),
                              ),
                            ),
                          ],
                        ),
                      ),
                      Container(
                          width: 1,
                          height: 44.h,
                          color: Colors.white.withValues(alpha: 0.20)),
                      Expanded(
                        child: Column(
                          children: [
                            Text(
                              'TARGET',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 9.sp,
                                fontWeight: FontWeight.w700,
                                letterSpacing: 1.5,
                                color: Colors.white.withValues(alpha: 0.65),
                              ),
                            ),
                            SizedBox(height: 3.h),
                            Text(
                              _fmtCurrency(totalTarget),
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 22.sp,
                                fontWeight: FontWeight.w900,
                                height: 1.0,
                                letterSpacing: -0.5,
                                color: Colors.white,
                              ),
                            ),
                            Text(
                              'LKR',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 9.sp,
                                fontWeight: FontWeight.w600,
                                letterSpacing: 1.0,
                                color: Colors.white.withValues(alpha: 0.55),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),

                  SizedBox(height: 14.h),
                  Container(
                      height: 1,
                      color: Colors.white.withValues(alpha: 0.15)),
                  SizedBox(height: 10.h),

                  // Daily pace label
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Container(
                          width: 20.w,
                          height: 1,
                          color: Colors.white.withValues(alpha: 0.25)),
                      SizedBox(width: 8.w),
                      Text(
                        isCurrentMonth
                            ? 'DAILY PACE  ·  DAY $daysElapsed OF $daysInMonth'
                            : 'DAILY AVERAGE  ·  $daysInMonth DAYS',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 9.sp,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 1.8,
                          color: Colors.white.withValues(alpha: 0.50),
                        ),
                      ),
                      SizedBox(width: 8.w),
                      Container(
                          width: 20.w,
                          height: 1,
                          color: Colors.white.withValues(alpha: 0.25)),
                    ],
                  ),
                  SizedBox(height: 10.h),

                  // Daily target | daily sales | daily % row
                  Row(
                    children: [
                      Expanded(
                        child: Column(
                          children: [
                            Text(
                              'DAILY TARGET',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 8.sp,
                                fontWeight: FontWeight.w700,
                                letterSpacing: 1.2,
                                color: Colors.white.withValues(alpha: 0.55),
                              ),
                            ),
                            SizedBox(height: 2.h),
                            Text(
                              _fmtCurrency(dailyTarget),
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 16.sp,
                                fontWeight: FontWeight.w900,
                                height: 1.0,
                                letterSpacing: -0.3,
                                color: Colors.white.withValues(alpha: 0.85),
                              ),
                            ),
                            Text(
                              'LKR / DAY',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 8.sp,
                                fontWeight: FontWeight.w600,
                                letterSpacing: 0.8,
                                color: Colors.white.withValues(alpha: 0.40),
                              ),
                            ),
                          ],
                        ),
                      ),
                      Container(
                          width: 1,
                          height: 36.h,
                          color: Colors.white.withValues(alpha: 0.18)),
                      Expanded(
                        child: Column(
                          children: [
                            Text(
                              'DAILY SALES',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 8.sp,
                                fontWeight: FontWeight.w700,
                                letterSpacing: 1.2,
                                color: Colors.white.withValues(alpha: 0.55),
                              ),
                            ),
                            SizedBox(height: 2.h),
                            Text(
                              _fmtCurrency(dailySales),
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 16.sp,
                                fontWeight: FontWeight.w900,
                                height: 1.0,
                                letterSpacing: -0.3,
                                color: Colors.white.withValues(alpha: 0.85),
                              ),
                            ),
                            Text(
                              'LKR / DAY',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 8.sp,
                                fontWeight: FontWeight.w600,
                                letterSpacing: 0.8,
                                color: Colors.white.withValues(alpha: 0.40),
                              ),
                            ),
                          ],
                        ),
                      ),
                      Container(
                          width: 1,
                          height: 36.h,
                          color: Colors.white.withValues(alpha: 0.18)),
                      Expanded(
                        child: Column(
                          children: [
                            Text(
                              'DAILY %',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 8.sp,
                                fontWeight: FontWeight.w700,
                                letterSpacing: 1.2,
                                color: Colors.white.withValues(alpha: 0.55),
                              ),
                            ),
                            SizedBox(height: 2.h),
                            Text(
                              '${dailyPct.toStringAsFixed(0)}%',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 16.sp,
                                fontWeight: FontWeight.w900,
                                height: 1.0,
                                letterSpacing: -0.3,
                                color: Colors.white.withValues(alpha: 0.85),
                              ),
                            ),
                            Text(
                              'ACHIEVED',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 8.sp,
                                fontWeight: FontWeight.w600,
                                letterSpacing: 0.8,
                                color: Colors.white.withValues(alpha: 0.40),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
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
              width: 1, height: 30.h, color: AppColors.surfaceVariant),
          Expanded(
            child: _MiniStat(
              label: 'SOLD',
              value:
                  '${_fmtCases(data.totalSoldQuantity)} CS · ${_fmtPacks(data.totalSoldQuantityPacks)} PKT',
            ),
          ),
          Container(
              width: 1, height: 30.h, color: AppColors.surfaceVariant),
          Expanded(
            child: Center(
              child: Container(
                padding:
                    EdgeInsets.symmetric(horizontal: 10.w, vertical: 5.h),
                decoration: BoxDecoration(
                  color: accent.withValues(alpha: 0.10),
                  borderRadius: BorderRadius.circular(999),
                  border:
                      Border.all(color: accent.withValues(alpha: 0.30)),
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
  const _QtyChip(
      {required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(label,
            style: GoogleFonts.barlowCondensed(
              fontSize: 9.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.5,
              color: AppColors.foregroundMuted,
            )),
        Text(value,
            style: GoogleFonts.barlowCondensed(
              fontSize: 14.sp,
              fontWeight: FontWeight.w800,
              height: 1.1,
              color: color,
            )),
      ],
    );
  }
}

class _SoldChip extends StatelessWidget {
  final double cases;
  final double packs;
  final Color color;
  const _SoldChip(
      {required this.cases, required this.packs, required this.color});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text('SOLD',
            style: GoogleFonts.barlowCondensed(
              fontSize: 9.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.5,
              color: AppColors.foregroundMuted,
            )),
        Text('${_fmtCases(cases)} CS',
            style: GoogleFonts.barlowCondensed(
              fontSize: 14.sp,
              fontWeight: FontWeight.w800,
              height: 1.1,
              color: color,
            )),
        Text('${_fmtPacks(packs)} PKT',
            style: GoogleFonts.barlowCondensed(
              fontSize: 11.sp,
              fontWeight: FontWeight.w600,
              height: 1.2,
              color: AppColors.foregroundMuted,
            )),
      ],
    );
  }
}

// ── Empty view ────────────────────────────────────────────────────────────────

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
              'Nothing recorded for this rep and month.',
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

// ── Formatting helpers ────────────────────────────────────────────────────────

String _fmtCurrency(double v) {
  final whole = v.toStringAsFixed(0);
  final buf = StringBuffer();
  for (int i = 0; i < whole.length; i++) {
    if (i > 0 && (whole.length - i) % 3 == 0) buf.write(',');
    buf.write(whole[i]);
  }
  return buf.toString();
}

String _fmtCases(double v) {
  if (v == v.roundToDouble()) return v.toStringAsFixed(0);
  return v.toStringAsFixed(1);
}

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
