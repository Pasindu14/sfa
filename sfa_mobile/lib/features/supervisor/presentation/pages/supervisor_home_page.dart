import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:uswatte/features/supervisor_summary/presentation/cubit/supervisor_summary_cubit.dart';
import 'package:uswatte/features/supervisor_summary/presentation/cubit/supervisor_summary_state.dart';


class SupervisorHomePage extends StatefulWidget {
  const SupervisorHomePage({super.key});

  @override
  State<SupervisorHomePage> createState() => _SupervisorHomePageState();
}

class _SupervisorHomePageState extends State<SupervisorHomePage>
    with SingleTickerProviderStateMixin {
  late AnimationController _ctrl;

  Animation<double> _fade(double from, double to) => CurvedAnimation(
      parent: _ctrl, curve: Interval(from, to, curve: Curves.easeOut));

  Animation<Offset> _slide(double from, double to) =>
      Tween<Offset>(begin: const Offset(0, 0.07), end: Offset.zero).animate(
          CurvedAnimation(
              parent: _ctrl,
              curve: Interval(from, to, curve: Curves.easeOutCubic)));

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
        vsync: this, duration: const Duration(milliseconds: 900))
      ..forward();
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.dark,
    ));

    return Scaffold(
      backgroundColor: AppColors.background,
      body: RefreshIndicator(
        color: AppColors.primary,
        onRefresh: () async =>
            context.read<SupervisorSummaryCubit>().refresh(),
        child: CustomScrollView(
        slivers: [
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.0, 0.5),
              child: const _TopBar(),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.05, 0.55),
              child: SlideTransition(
                  position: _slide(0.05, 0.55), child: const _HeroCard()),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.15, 0.65),
              child: SlideTransition(
                position: _slide(0.15, 0.65),
                child: const _SectionLabel("TODAY'S TEAM"),
              ),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.20, 0.70),
              child: SlideTransition(
                position: _slide(0.20, 0.70),
                child: const _MetricsSection(),
              ),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.35, 0.80),
              child: SlideTransition(
                position: _slide(0.35, 0.80),
                child: const _SectionLabel('QUICK ACTIONS'),
              ),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.45, 0.90),
              child: SlideTransition(
                position: _slide(0.45, 0.90),
                child: const _ActionsSection(),
              ),
            ),
          ),
          SliverToBoxAdapter(child: SizedBox(height: 40.h)),
        ],
      ),
      ),
    );
  }
}

// ── Top bar ───────────────────────────────────────────────────────────────────
class _TopBar extends StatelessWidget {
  const _TopBar();

  String get _dateLabel {
    final now = DateTime.now();
    const m = ['JAN','FEB','MAR','APR','MAY','JUN',
                'JUL','AUG','SEP','OCT','NOV','DEC'];
    const d = ['MON','TUE','WED','THU','FRI','SAT','SUN'];
    return '${d[now.weekday - 1]}, ${m[now.month - 1]} ${now.day}';
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Container(
        color: AppColors.background,
        padding: EdgeInsets.fromLTRB(20.w, 12.h, 20.w, 8.h),
        child: Row(
          children: [
            Image.asset('assets/images/uswatte-logo.png',
                height: 32.h, fit: BoxFit.contain),
            SizedBox(width: 10.w),
            Text('SFA',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 11.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 4,
                  color: AppColors.foregroundMuted,
                )),
            const Spacer(),
            Text(_dateLabel,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 11.sp,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 1.0,
                  color: AppColors.foregroundMuted,
                )),
            SizedBox(width: 12.w),
            _NavIconBtn(
              icon: Icons.logout_rounded,
              onTap: () =>
                  context.read<AuthBloc>().add(const LogoutRequested()),
              accent: true,
            ),
          ],
        ),
      ),
    );
  }
}

class _NavIconBtn extends StatelessWidget {
  const _NavIconBtn(
      {required this.icon, required this.onTap, this.accent = false});
  final IconData icon;
  final VoidCallback onTap;
  final bool accent;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 34.r,
        height: 34.r,
        decoration: BoxDecoration(
          color: accent
              ? AppColors.primary.withValues(alpha: 0.08)
              : AppColors.surface,
          borderRadius: BorderRadius.circular(8.r),
          border: Border.all(
            color: accent
                ? AppColors.primary.withValues(alpha: 0.25)
                : AppColors.surfaceVariant,
          ),
        ),
        child: Icon(icon,
            size: 16.r,
            color: accent ? AppColors.primary : AppColors.foregroundMuted),
      ),
    );
  }
}

// ── Hero greeting card ────────────────────────────────────────────────────────
class _HeroCard extends StatelessWidget {
  const _HeroCard();

  String get _greeting {
    final hour = DateTime.now().hour;
    if (hour < 12) return 'Good morning,';
    if (hour < 17) return 'Good afternoon,';
    return 'Good evening,';
  }

  @override
  Widget build(BuildContext context) {
    final name = context.select<AuthBloc, String>(
      (bloc) => bloc.state is AuthAuthenticated
          ? (bloc.state as AuthAuthenticated).name
          : '',
    );
    final displayName = name.isNotEmpty ? name : 'Supervisor';

    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 0),
      child: Container(
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
        padding: EdgeInsets.fromLTRB(22.w, 22.h, 22.w, 22.h),
        child: Stack(
          clipBehavior: Clip.none,
          children: [
            Positioned(
              right: -20.w,
              top: -20.h,
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
              right: 20.w,
              bottom: -10.h,
              child: Container(
                width: 70.r,
                height: 70.r,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: Colors.white.withValues(alpha: 0.05),
                ),
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 4.h),
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.18),
                    borderRadius: BorderRadius.circular(20.r),
                  ),
                  child: Text('SUPERVISOR',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 10.sp,
                        fontWeight: FontWeight.w700,
                        letterSpacing: 2.5,
                        color: Colors.white,
                      )),
                ),
                SizedBox(height: 14.h),
                Text(_greeting,
                    style: GoogleFonts.barlow(
                      fontSize: 13.sp,
                      color: Colors.white.withValues(alpha: 0.75),
                    )),
                Text(displayName,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 32.sp,
                      fontWeight: FontWeight.w800,
                      letterSpacing: -0.5,
                      height: 1.0,
                      color: Colors.white,
                    )),
                SizedBox(height: 16.h),
                Row(
                  children: [
                    Container(
                        height: 2.h,
                        width: 20.w,
                        color: Colors.white.withValues(alpha: 0.5)),
                    SizedBox(width: 4.w),
                    Container(
                        height: 2.h,
                        width: 7.w,
                        color: Colors.white.withValues(alpha: 0.25)),
                  ],
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

// ── Today's team metrics ──────────────────────────────────────────────────────
class _MetricsSection extends StatelessWidget {
  const _MetricsSection();

  String _formatSales(double amount) {
    final whole = amount.truncate();
    final cents = ((amount - whole) * 100).round();
    final s = whole.toString();
    final buf = StringBuffer();
    final offset = s.length % 3;
    for (var i = 0; i < s.length; i++) {
      if (i > 0 && (i - offset) % 3 == 0) buf.write(',');
      buf.write(s[i]);
    }
    return 'Rs. $buf.${cents.toString().padLeft(2, '0')}';
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 0, 16.w, 0),
      child: BlocBuilder<SupervisorSummaryCubit, SupervisorSummaryState>(
        buildWhen: (prev, curr) => curr != prev,
        builder: (context, state) {
          final summary = state is SupervisorSummaryLoaded ? state.summary : null;
          final loading = state is SupervisorSummaryLoading;
          final hasError = state is SupervisorSummaryError;

          if (hasError) {
            return GestureDetector(
              onTap: () => context.read<SupervisorSummaryCubit>().refresh(),
              child: Container(
                padding: EdgeInsets.symmetric(vertical: 20.h, horizontal: 12.w),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(12.r),
                  border: Border.all(color: AppColors.surfaceVariant),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.refresh_rounded,
                        size: 16.r, color: AppColors.primary),
                    SizedBox(width: 8.w),
                    Text(
                      'Could not load data — tap to retry',
                      style: GoogleFonts.barlow(
                        fontSize: 13.sp,
                        color: AppColors.foregroundMuted,
                      ),
                    ),
                  ],
                ),
              ),
            );
          }

          return Column(
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: _MetricTile(
                      icon: Icons.people_rounded,
                      label: 'Reps\nAssigned',
                      value: loading
                          ? '…'
                          : summary != null
                              ? '${summary.assignedReps}/${summary.totalReps}'
                              : '—',
                      large: true,
                    ),
                  ),
                  SizedBox(width: 10.w),
                  Expanded(
                    child: Column(
                      children: [
                        _MetricTile(
                          icon: Icons.receipt_long_rounded,
                          label: 'Bills Today',
                          value: loading
                              ? '…'
                              : summary != null
                                  ? '${summary.billsToday}'
                                  : '—',
                          large: false,
                        ),
                        SizedBox(height: 10.h),
                        _MetricTile(
                          icon: Icons.block_rounded,
                          label: 'Non-Billings',
                          value: loading
                              ? '…'
                              : summary != null
                                  ? '${summary.nonBillingsToday}'
                                  : '—',
                          large: false,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              SizedBox(height: 10.h),
              _SalesTile(
                value: loading
                    ? '…'
                    : summary != null
                        ? _formatSales(summary.totalSalesToday)
                        : '—',
                loading: loading,
              ),
            ],
          );
        },
      ),
    );
  }
}

// ── Total sales wide tile ─────────────────────────────────────────────────────
class _SalesTile extends StatelessWidget {
  const _SalesTile({required this.value, required this.loading});

  final String value;
  final bool loading;

  @override
  Widget build(BuildContext context) {
    const color = AppColors.success;
    return Container(
      width: double.infinity,
      padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 14.h),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: color.withValues(alpha: 0.18)),
        boxShadow: [
          BoxShadow(
            color: color.withValues(alpha: 0.07),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Row(
        children: [
          Container(
            width: 34.r,
            height: 34.r,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8.r),
            ),
            child: Icon(Icons.trending_up_rounded, color: color, size: 17.r),
          ),
          SizedBox(width: 12.w),
          Expanded(
            child: Text(
              'Total Sales Today',
              style: GoogleFonts.barlowCondensed(
                fontSize: 12.sp,
                fontWeight: FontWeight.w600,
                color: AppColors.foregroundMuted,
                letterSpacing: 0.3,
              ),
            ),
          ),
          loading
              ? SizedBox(
                  width: 16.r,
                  height: 16.r,
                  child: CircularProgressIndicator(
                      strokeWidth: 1.5, color: color),
                )
              : Text(
                  value,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 22.sp,
                    fontWeight: FontWeight.w900,
                    letterSpacing: -0.5,
                    height: 1.0,
                    color: color,
                  ),
                ),
        ],
      ),
    );
  }
}

class _MetricTile extends StatelessWidget {
  const _MetricTile({
    required this.icon,
    required this.label,
    required this.value,
    required this.large,
  });
  final IconData icon;
  final String label, value;
  final bool large;

  @override
  Widget build(BuildContext context) {
    const color = AppColors.primary;
    return Container(
      height: large ? 158.h : 74.h,
      padding: EdgeInsets.all(14.r),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.surfaceVariant),
        boxShadow: [
          BoxShadow(
            color: color.withValues(alpha: 0.06),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: large
          ? Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Container(
                  width: 34.r,
                  height: 34.r,
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(8.r),
                  ),
                  child: Icon(icon, color: color, size: 17.r),
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(value,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 46.sp,
                          fontWeight: FontWeight.w900,
                          height: 1.0,
                          letterSpacing: -1.5,
                          color: color,
                        )),
                    Text(label,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 12.sp,
                          fontWeight: FontWeight.w600,
                          height: 1.25,
                          color: AppColors.foregroundMuted,
                        )),
                  ],
                ),
              ],
            )
          : Row(
              children: [
                Icon(icon, color: color, size: 16.r),
                SizedBox(width: 8.w),
                Expanded(
                  child: Text(label,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 11.sp,
                        fontWeight: FontWeight.w600,
                        height: 1.2,
                        color: AppColors.foregroundMuted,
                      )),
                ),
                Text(value,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 24.sp,
                      fontWeight: FontWeight.w900,
                      height: 1.0,
                      letterSpacing: -1,
                      color: color,
                    )),
              ],
            ),
    );
  }
}

// ── Section label ─────────────────────────────────────────────────────────────
class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.text);
  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(20.w, 22.h, 20.w, 10.h),
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
          Text(text,
              style: GoogleFonts.barlowCondensed(
                fontSize: 11.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 2.5,
                color: AppColors.foregroundMuted,
              )),
        ],
      ),
    );
  }
}

// ── Actions ───────────────────────────────────────────────────────────────────
class _ActionsSection extends StatelessWidget {
  const _ActionsSection();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: 16.w),
      child: Column(
        children: [
          _HeroActionCard(
            icon: Icons.map_rounded,
            title: 'ASSIGN DAILY ROUTE',
            subtitle: 'Schedule a route for a sales rep',
            onTap: () async {
              await context.push('/supervisor/assign-route');
              if (context.mounted) {
                context.read<SupervisorSummaryCubit>().refresh();
              }
            },
          ),
          SizedBox(height: 12.h),
          Row(
            children: [
              Expanded(
                child: _TileActionCard(
                  icon: Icons.list_alt_rounded,
                  title: 'VIEW\nASSIGNMENTS',
                  subtitle: "Today's route assignments",
                  color: AppColors.primary,
                  onTap: () async {
                    await context.push('/supervisor/assignments');
                    if (context.mounted) {
                      context.read<SupervisorSummaryCubit>().refresh();
                    }
                  },
                ),
              ),
              SizedBox(width: 10.w),
              Expanded(
                child: _TileActionCard(
                  icon: Icons.receipt_long_rounded,
                  title: 'VIEW REP\nBILLS',
                  subtitle: 'Bills created by your reps',
                  color: AppColors.primary,
                  onTap: () async {
                    await context.push('/supervisor/billing');
                    if (context.mounted) {
                      context.read<SupervisorSummaryCubit>().refresh();
                    }
                  },
                ),
              ),
            ],
          ),
          SizedBox(height: 10.h),
          Row(
            children: [
              Expanded(
                child: _TileActionCard(
                  icon: Icons.block_rounded,
                  title: 'VIEW\nNON-BILLINGS',
                  subtitle: 'Non-billing visits by reps',
                  color: AppColors.primary,
                  onTap: () async {
                    await context.push('/supervisor/not-billing');
                    if (context.mounted) {
                      context.read<SupervisorSummaryCubit>().refresh();
                    }
                  },
                ),
              ),
              SizedBox(width: 10.w),
              Expanded(
                child: _TileActionCard(
                  icon: Icons.map_rounded,
                  title: "REP ROUTE\nMAP",
                  subtitle: "View a rep's route on map",
                  color: AppColors.primary,
                  onTap: () => context.push('/supervisor/rep-route-map'),
                ),
              ),
            ],
          ),
          SizedBox(height: 10.h),
          _TileActionCard(
            icon: Icons.emoji_events_rounded,
            title: 'REP ACHIEVEMENT',
            subtitle: 'Item-wise target vs sold by month',
            color: AppColors.primary,
            onTap: () => context.push('/supervisor/achievement'),
          ),
        ],
      ),
    );
  }
}

// ── Hero action card (primary CTA) ────────────────────────────────────────────
class _HeroActionCard extends StatelessWidget {
  const _HeroActionCard({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });
  final IconData icon;
  final String title, subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16.r),
        child: Ink(
          height: 92.h,
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [AppColors.primaryDark, AppColors.primary, AppColors.primaryLight],
              stops: const [0.0, 0.55, 1.0],
            ),
            borderRadius: BorderRadius.circular(16.r),
            boxShadow: [
              BoxShadow(
                color: AppColors.primary.withValues(alpha: 0.38),
                blurRadius: 22,
                offset: const Offset(0, 8),
              ),
            ],
          ),
          child: Stack(
            clipBehavior: Clip.none,
            children: [
              Positioned(
                right: -12.w,
                top: -18.h,
                child: Container(
                  width: 110.r,
                  height: 110.r,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: Colors.white.withValues(alpha: 0.07),
                  ),
                ),
              ),
              Positioned(
                right: 50.w,
                bottom: -14.h,
                child: Container(
                  width: 55.r,
                  height: 55.r,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: Colors.white.withValues(alpha: 0.05),
                  ),
                ),
              ),
              Padding(
                padding: EdgeInsets.symmetric(horizontal: 22.w),
                child: Row(
                  children: [
                    Container(
                      width: 52.r,
                      height: 52.r,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(14.r),
                        border: Border.all(
                          color: Colors.white.withValues(alpha: 0.22),
                        ),
                      ),
                      child: Icon(icon, color: Colors.white, size: 24.r),
                    ),
                    SizedBox(width: 18.w),
                    Expanded(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            padding: EdgeInsets.symmetric(
                                horizontal: 7.w, vertical: 2.h),
                            decoration: BoxDecoration(
                              color: Colors.white.withValues(alpha: 0.18),
                              borderRadius: BorderRadius.circular(4.r),
                            ),
                            child: Text('PRIMARY ACTION',
                                style: GoogleFonts.barlowCondensed(
                                  fontSize: 8.sp,
                                  fontWeight: FontWeight.w700,
                                  letterSpacing: 1.5,
                                  color: Colors.white.withValues(alpha: 0.88),
                                )),
                          ),
                          SizedBox(height: 5.h),
                          Text(title,
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 23.sp,
                                fontWeight: FontWeight.w900,
                                letterSpacing: 1.0,
                                height: 1.0,
                                color: Colors.white,
                              )),
                          SizedBox(height: 3.h),
                          Text(subtitle,
                              style: GoogleFonts.barlow(
                                fontSize: 11.sp,
                                color: Colors.white.withValues(alpha: 0.72),
                              )),
                        ],
                      ),
                    ),
                    Container(
                      width: 32.r,
                      height: 32.r,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.15),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(Icons.arrow_forward_rounded,
                          color: Colors.white, size: 15.r),
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

// ── Tile action card (secondary CTA) ─────────────────────────────────────────
class _TileActionCard extends StatelessWidget {
  const _TileActionCard({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.color,
    this.onTap,
  });
  final IconData icon;
  final String title, subtitle;
  final Color color;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(14.r),
        child: Ink(
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(14.r),
            border: Border.all(color: color.withValues(alpha: 0.14)),
            boxShadow: [
              BoxShadow(
                color: color.withValues(alpha: 0.09),
                blurRadius: 14,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                height: 64.h,
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.09),
                  borderRadius: BorderRadius.only(
                    topLeft: Radius.circular(14.r),
                    topRight: Radius.circular(14.r),
                  ),
                ),
                child: Stack(
                  clipBehavior: Clip.none,
                  children: [
                    Positioned(
                      right: -10.w,
                      top: -10.h,
                      child: Container(
                        width: 54.r,
                        height: 54.r,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: color.withValues(alpha: 0.07),
                        ),
                      ),
                    ),
                    Padding(
                      padding: EdgeInsets.fromLTRB(14.w, 14.h, 14.w, 0),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            width: 36.r,
                            height: 36.r,
                            decoration: BoxDecoration(
                              color: color.withValues(alpha: 0.16),
                              borderRadius: BorderRadius.circular(10.r),
                            ),
                            child: Icon(icon, color: color, size: 18.r),
                          ),
                          Icon(Icons.arrow_outward_rounded,
                              color: color.withValues(alpha: 0.40), size: 14.r),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              Padding(
                padding: EdgeInsets.fromLTRB(14.w, 10.h, 14.w, 14.h),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 14.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 0.6,
                          height: 1.0,
                          color: AppColors.foreground,
                        )),
                    SizedBox(height: 3.h),
                    Text(subtitle,
                        style: GoogleFonts.barlow(
                          fontSize: 10.sp,
                          color: AppColors.foregroundMuted,
                        )),
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
