import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:go_router/go_router.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_bloc.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_state.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_bloc.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_event.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_state.dart';
import 'package:uswatte/features/rep_assignment/presentation/bloc/rep_assignment_bloc.dart';
import 'package:uswatte/features/route_assignment/presentation/bloc/assignments_bloc.dart';
import 'package:uswatte/features/sales_rep_target/presentation/cubit/rep_target_cubit.dart';
import 'package:uswatte/features/sales_rep_target/presentation/cubit/rep_target_state.dart';
import 'package:uswatte/features/rep_monthly_sales/presentation/cubit/rep_daily_sales_cubit.dart';
import 'package:uswatte/features/rep_monthly_sales/presentation/cubit/rep_daily_sales_state.dart';
import 'package:uswatte/features/rep_monthly_sales/presentation/cubit/rep_monthly_sales_cubit.dart';
import 'package:uswatte/features/rep_monthly_sales/presentation/cubit/rep_monthly_sales_state.dart';

class SalesRepHomePage extends StatefulWidget {
  const SalesRepHomePage({super.key});

  @override
  State<SalesRepHomePage> createState() => _SalesRepHomePageState();
}

class _SalesRepHomePageState extends State<SalesRepHomePage>
    with SingleTickerProviderStateMixin, WidgetsBindingObserver {
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
    WidgetsBinding.instance.addObserver(this);
    _ctrl = AnimationController(
        vsync: this, duration: const Duration(milliseconds: 1000))
      ..forward();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed && mounted) {
      context
          .read<AssignmentsBloc>()
          .add(LoadAssignmentsRequested(date: DateTime.now()));
    }
  }

  Future<void> _onRefresh() async {
    final now = DateTime.now();
    context.read<AssignmentsBloc>().add(LoadAssignmentsRequested(date: now));
    await Future.wait([
      context.read<RepMonthlySalesCubit>().load(now.year, now.month),
      context.read<RepDailySalesCubit>().load(now),
      context.read<RepTargetCubit>().load(now.year, now.month),
    ]);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.dark,
    ));

    return BlocListener<AssignmentsBloc, AssignmentsState>(
      listenWhen: (_, curr) =>
          curr is AssignmentsLoaded && curr.assignments.isNotEmpty,
      listener: (context, state) {
        if (state is AssignmentsLoaded && state.assignments.isNotEmpty) {
          final assignment = state.assignments.first;
          context.read<OutletsBloc>().add(SyncDailyOutletsRequested(
                routeId: assignment.routeId,
                routeName: assignment.routeName,
              ));
        }
      },
      child: Scaffold(
      backgroundColor: AppColors.background,
      body: RefreshIndicator(
        color: AppColors.primary,
        onRefresh: _onRefresh,
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
                  position: _slide(0.05, 0.55), child: const _HeroRow()),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.10, 0.60),
              child: SlideTransition(
                  position: _slide(0.10, 0.60),
                  child: const _NewOrderButton()),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.15, 0.65),
              child: SlideTransition(
                  position: _slide(0.15, 0.65), child: const _KpiRow()),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.20, 0.70),
              child: SlideTransition(
                  position: _slide(0.20, 0.70),
                  child: const _DailyPaceRow()),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.25, 0.75),
              child: SlideTransition(
                  position: _slide(0.25, 0.75),
                  child: const _SectionLabel("TODAY'S METRICS")),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.30, 0.80),
              child: SlideTransition(
                  position: _slide(0.30, 0.80),
                  child: const _MetricsGrid()),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.45, 0.90),
              child: SlideTransition(
                  position: _slide(0.45, 0.90),
                  child: const _SectionLabel('QUICK ACTIONS')),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.50, 1.0),
              child: SlideTransition(
                  position: _slide(0.50, 1.0),
                  child: const _ActionsGrid()),
            ),
          ),
          SliverToBoxAdapter(child: SizedBox(height: 40.h)),
        ],
      ),
      ),
      ),
    );
  }
}

// ── New Order CTA ─────────────────────────────────────────────────────────────
class _NewOrderButton extends StatefulWidget {
  const _NewOrderButton();

  @override
  State<_NewOrderButton> createState() => _NewOrderButtonState();
}

class _NewOrderButtonState extends State<_NewOrderButton>
    with SingleTickerProviderStateMixin {
  late final AnimationController _pulse;
  late final Animation<double> _glow;

  @override
  void initState() {
    super.initState();
    _pulse = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1800),
    )..repeat(reverse: true);
    _glow = Tween<double>(begin: 0.0, end: 1.0).animate(
      CurvedAnimation(parent: _pulse, curve: Curves.easeInOut),
    );
  }

  @override
  void dispose() {
    _pulse.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 10.h, 16.w, 0),
      child: AnimatedBuilder(
        animation: _glow,
        builder: (context, child) => Container(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16.r),
            boxShadow: [
              BoxShadow(
                color: AppColors.primary
                    .withValues(alpha: 0.28 + _glow.value * 0.18),
                blurRadius: 18 + _glow.value * 14,
                offset: const Offset(0, 6),
                spreadRadius: _glow.value * 1.5,
              ),
            ],
          ),
          child: child,
        ),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: () => context.push('/sales-rep/bills/create'),
            borderRadius: BorderRadius.circular(16.r),
            splashColor: Colors.white.withValues(alpha: 0.14),
            highlightColor: Colors.white.withValues(alpha: 0.07),
            child: Ink(
              height: 74.h,
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.centerLeft,
                  end: Alignment.centerRight,
                  colors: [
                    AppColors.primaryDark,
                    AppColors.primary,
                    AppColors.primaryLight,
                  ],
                  stops: const [0.0, 0.5, 1.0],
                ),
                borderRadius: BorderRadius.circular(16.r),
              ),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(16.r),
                child: Stack(
                  children: [
                    Positioned.fill(
                      child: CustomPaint(painter: _DiagonalStripePainter()),
                    ),
                    Padding(
                      padding: EdgeInsets.symmetric(horizontal: 20.w),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('+ PLACE',
                                    style: GoogleFonts.barlowCondensed(
                                      fontSize: 9.sp,
                                      fontWeight: FontWeight.w700,
                                      letterSpacing: 3.0,
                                      color:
                                          Colors.white.withValues(alpha: 0.62),
                                    )),
                                Text('NEW ORDER',
                                    style: GoogleFonts.barlowCondensed(
                                      fontSize: 30.sp,
                                      fontWeight: FontWeight.w900,
                                      letterSpacing: 0.8,
                                      height: 1.0,
                                      color: Colors.white,
                                    )),
                              ],
                            ),
                          ),
                          Container(
                            width: 46.r,
                            height: 46.r,
                            decoration: BoxDecoration(
                              color: Colors.white.withValues(alpha: 0.17),
                              shape: BoxShape.circle,
                              border: Border.all(
                                color: Colors.white.withValues(alpha: 0.28),
                                width: 1.5,
                              ),
                            ),
                            child: Icon(Icons.add_shopping_cart_rounded,
                                color: Colors.white, size: 20.r),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _DiagonalStripePainter extends CustomPainter {
  const _DiagonalStripePainter();

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = Colors.white.withValues(alpha: 0.055)
      ..strokeWidth = 10;
    const spacing = 22.0;
    for (double x = -size.height; x < size.width + size.height; x += spacing) {
      canvas.drawLine(
        Offset(x, 0),
        Offset(x + size.height, size.height),
        paint,
      );
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter old) => false;
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
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
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
                  icon: Icons.notifications_outlined,
                  onTap: () => context.push('/sales-rep/notifications'),
                ),
                SizedBox(width: 6.w),
                _NavIconBtn(
                  icon: Icons.logout_rounded,
                  onTap: () =>
                      context.read<AuthBloc>().add(const LogoutRequested()),
                  accent: true,
                ),
              ],
            ),
            BlocBuilder<AssignmentsBloc, AssignmentsState>(
              builder: (context, state) {
                if (state is AssignmentsLoaded &&
                    state.assignments.isNotEmpty) {
                  return _RouteChip(
                      routeName: state.assignments.first.routeName);
                }
                return const SizedBox.shrink();
              },
            ),
          ],
        ),
      ),
    );
  }
}

class _RouteChip extends StatelessWidget {
  const _RouteChip({required this.routeName});
  final String routeName;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(top: 8.h),
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 5.h),
        decoration: BoxDecoration(
          color: AppColors.primary.withValues(alpha: 0.08),
          borderRadius: BorderRadius.circular(8.r),
          border: Border.all(
            color: AppColors.primary.withValues(alpha: 0.22),
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.route_rounded, size: 12.r, color: AppColors.primary),
            SizedBox(width: 6.w),
            Text(
              "TODAY'S ROUTE",
              style: GoogleFonts.barlowCondensed(
                fontSize: 9.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 1.5,
                color: AppColors.primary,
              ),
            ),
            Container(
              margin: EdgeInsets.symmetric(horizontal: 8.w),
              width: 1,
              height: 10.h,
              color: AppColors.primary.withValues(alpha: 0.30),
            ),
            Flexible(
              child: Text(
                routeName,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 12.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.3,
                  color: AppColors.foreground,
                ),
                overflow: TextOverflow.ellipsis,
              ),
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
            color:
                accent ? AppColors.primary : AppColors.foregroundMuted),
      ),
    );
  }
}

// ── Hero row (greeting + achievement) ────────────────────────────────────────
class _HeroRow extends StatelessWidget {
  const _HeroRow();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 0),
      child: IntrinsicHeight(
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Expanded(child: _HeroCard()),
            SizedBox(width: 10.w),
            const _AchievementCard(),
          ],
        ),
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
    final displayName = name.isNotEmpty ? name : 'Sales Rep';

    final distributorName = context.select<RepAssignmentBloc, String?>(
      (bloc) => bloc.state is RepAssignmentLoaded
          ? (bloc.state as RepAssignmentLoaded).assignment.distributorName
          : null,
    );

    return Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
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
                  padding: EdgeInsets.symmetric(
                      horizontal: 10.w, vertical: 4.h),
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.18),
                    borderRadius: BorderRadius.circular(20.r),
                  ),
                  child: Text('DASHBOARD',
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
                if (distributorName != null) ...[
                  SizedBox(height: 6.h),
                  Row(
                    children: [
                      Icon(Icons.store_rounded,
                          size: 11.r,
                          color: Colors.white.withValues(alpha: 0.7)),
                      SizedBox(width: 5.w),
                      Text(
                        distributorName,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 12.sp,
                          fontWeight: FontWeight.w600,
                          letterSpacing: 0.3,
                          color: Colors.white.withValues(alpha: 0.85),
                        ),
                      ),
                    ],
                  ),
                ],
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
    );
  }
}

// ── Achievement card (MTD %) ──────────────────────────────────────────────────
class _AchievementCard extends StatelessWidget {
  const _AchievementCard();

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<RepMonthlySalesCubit, RepMonthlySalesState>(
      builder: (context, salesState) =>
          BlocBuilder<RepTargetCubit, RepTargetState>(
        builder: (context, targetState) {
          final isLoading = salesState is RepMonthlySalesLoading ||
              targetState is RepTargetLoading;

          double? pct;
          if (salesState is RepMonthlySalesLoaded &&
              targetState is RepTargetLoaded) {
            final target = targetState.target.totalTarget;
            pct = target > 0
                ? (salesState.sales.totalSales / target * 100)
                    .clamp(0.0, 999.0)
                : 0.0;
          }

          final Color accent = pct == null
              ? AppColors.primary
              : pct >= 100
                  ? const Color(0xFF22C55E)
                  : pct >= 75
                      ? const Color(0xFFF59E0B)
                      : AppColors.primary;

          final progressVal =
              pct != null ? (pct / 100).clamp(0.0, 1.0) : 0.0;
          final label = isLoading
              ? '...'
              : pct != null
                  ? '${pct.toStringAsFixed(0)}%'
                  : '—';

          return GestureDetector(
            behavior: HitTestBehavior.opaque,
            onTap: () => context.pushNamed('achievementDetail'),
            child: Container(
            width: 96.w,
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16.r),
              border: Border.all(color: accent.withValues(alpha: 0.20)),
              boxShadow: [
                BoxShadow(
                  color: accent.withValues(alpha: 0.14),
                  blurRadius: 18,
                  offset: const Offset(0, 6),
                ),
              ],
            ),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Stack(
                  alignment: Alignment.center,
                  children: [
                    SizedBox(
                      width: 68.r,
                      height: 68.r,
                      child: CircularProgressIndicator(
                        value: progressVal,
                        strokeWidth: 6.r,
                        backgroundColor: accent.withValues(alpha: 0.12),
                        valueColor: AlwaysStoppedAnimation<Color>(accent),
                        strokeCap: StrokeCap.round,
                      ),
                    ),
                    Text(
                      label,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 20.sp,
                        fontWeight: FontWeight.w900,
                        height: 1.0,
                        letterSpacing: -0.5,
                        color: accent,
                      ),
                    ),
                  ],
                ),
                SizedBox(height: 10.h),
                Text(
                  'ACHIEVED',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 9.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 1.5,
                    color: AppColors.foregroundMuted,
                  ),
                ),
                SizedBox(height: 2.h),
                Text(
                  'THIS MONTH',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 8.sp,
                    fontWeight: FontWeight.w600,
                    letterSpacing: 1.0,
                    color: AppColors.foregroundMuted.withValues(alpha: 0.55),
                  ),
                ),
              ],
            ),
            ),
          );
        },
      ),
    );
  }
}

String _formatAmount(double amount) {
  final rounded = amount.toStringAsFixed(0);
  return rounded.replaceAllMapped(
    RegExp(r'(\d)(?=(\d{3})+(?!\d))'),
    (m) => '${m[1]},',
  );
}

// ── KPI row ───────────────────────────────────────────────────────────────────
class _KpiRow extends StatelessWidget {
  const _KpiRow();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 0),
      child: IntrinsicHeight(
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
          BlocBuilder<RepMonthlySalesCubit, RepMonthlySalesState>(
            builder: (context, state) {
              final value = switch (state) {
                RepMonthlySalesLoading() => '...',
                RepMonthlySalesLoaded(:final sales) => _formatAmount(sales.totalSales),
                _ => '—',
              };
              final pending = state is RepMonthlySalesLoaded && state.sales.pendingTotal > 0
                  ? '${_formatAmount(state.sales.pendingTotal)} PENDING'
                  : null;
              return _KpiCard(
                label: 'MTD SALES',
                value: value,
                unit: 'LKR  ·  APPROVED',
                color: AppColors.primary,
                pendingText: pending,
              );
            },
          ),
          SizedBox(width: 10.w),
          BlocBuilder<RepTargetCubit, RepTargetState>(
            builder: (context, state) {
              final value = switch (state) {
                RepTargetLoading() => '...',
                RepTargetLoaded(:final target) => _formatAmount(target.totalTarget),
                _ => '—',
              };
              return _KpiCard(
                  label: 'TARGET', value: value, unit: 'LKR',
                  color: AppColors.primary);
            },
          ),
        ],
      ),
      ),
    );
  }
}

class _KpiCard extends StatelessWidget {
  const _KpiCard({
    required this.label,
    required this.value,
    required this.unit,
    required this.color,
    this.pendingText,
  });
  final String label, value, unit;
  final Color color;
  final String? pendingText;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Container(
        padding: EdgeInsets.all(14.r),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.06),
          borderRadius: BorderRadius.circular(12.r),
          border: Border.all(color: color.withValues(alpha: 0.15)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 9.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 1.5,
                  color: color,
                )),
            SizedBox(height: 4.h),
            Text(value,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 26.sp,
                  fontWeight: FontWeight.w900,
                  height: 1.0,
                  letterSpacing: -0.5,
                  color: AppColors.foreground,
                )),
            Text(unit,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 9.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 1.0,
                  color: color,
                )),
            if (pendingText != null) ...[
              SizedBox(height: 4.h),
              Text(
                pendingText!,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 9.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 1.0,
                  color: const Color(0xFFF59E0B),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

// ── Daily pace row ────────────────────────────────────────────────────────────
class _DailyPaceRow extends StatelessWidget {
  const _DailyPaceRow();

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final daysInMonth = DateTime(now.year, now.month + 1, 0).day;
    final daysElapsed = now.day.clamp(1, daysInMonth);

    return BlocBuilder<RepDailySalesCubit, RepDailySalesState>(
      builder: (context, dailyState) =>
          BlocBuilder<RepTargetCubit, RepTargetState>(
        builder: (context, targetState) {
          final isLoading = dailyState is RepDailySalesLoading ||
              targetState is RepTargetLoading;

          double? dailyTarget;
          double? dailySales;
          double? dailyPct;

          if (!isLoading) {
            final totalTarget = targetState is RepTargetLoaded
                ? targetState.target.totalTarget
                : null;
            final dailyData = dailyState is RepDailySalesLoaded
                ? dailyState.sales
                : null;
            if (totalTarget != null && dailyData != null) {
              dailyTarget = totalTarget > 0 ? totalTarget / daysInMonth : 0.0;
              dailySales  = dailyData.pendingTotal;
              dailyPct    = dailyTarget > 0
                  ? (dailySales / dailyTarget * 100).clamp(0.0, 999.0)
                  : 0.0;
            }
          }

          final accent = dailyPct == null
              ? AppColors.primary
              : dailyPct >= 100
                  ? const Color(0xFF22C55E)
                  : dailyPct >= 75
                      ? const Color(0xFFF59E0B)
                      : AppColors.primary;

          String fmtVal(double? v) => v == null
              ? (isLoading ? '...' : '—')
              : _formatAmount(v);

          return Padding(
            padding: EdgeInsets.fromLTRB(16.w, 10.h, 16.w, 0),
            child: Container(
              padding: EdgeInsets.fromLTRB(14.w, 10.h, 14.w, 12.h),
              decoration: BoxDecoration(
                color: accent.withValues(alpha: 0.06),
                borderRadius: BorderRadius.circular(12.r),
                border: Border.all(color: accent.withValues(alpha: 0.18)),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.trending_up_rounded,
                          size: 11.r, color: accent),
                      SizedBox(width: 5.w),
                      Text(
                        'DAILY PACE  ·  DAY $daysElapsed OF $daysInMonth',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 9.sp,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 1.8,
                          color: accent,
                        ),
                      ),
                    ],
                  ),
                  SizedBox(height: 10.h),
                  Row(
                    children: [
                      _DailyStat(
                        label: 'DAILY TARGET',
                        value: fmtVal(dailyTarget),
                        unit: 'LKR / DAY',
                        color: AppColors.foregroundMuted,
                      ),
                      Container(
                          width: 1,
                          height: 36.h,
                          color: AppColors.surfaceVariant),
                      _DailyStat(
                        label: 'DAILY SALES',
                        value: fmtVal(dailySales),
                        unit: 'LKR  ·  PENDING',
                        color: AppColors.foreground,
                      ),
                      Container(
                          width: 1,
                          height: 36.h,
                          color: AppColors.surfaceVariant),
                      _DailyStat(
                        label: 'DAILY %',
                        value: dailyPct != null
                            ? '${dailyPct.toStringAsFixed(0)}%'
                            : (isLoading ? '...' : '—'),
                        unit: 'ACHIEVED',
                        color: accent,
                      ),
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}

class _DailyStat extends StatelessWidget {
  final String label;
  final String value;
  final String unit;
  final Color color;
  const _DailyStat({
    required this.label,
    required this.value,
    required this.unit,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(
        children: [
          Text(
            label,
            style: GoogleFonts.barlowCondensed(
              fontSize: 8.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.2,
              color: AppColors.foregroundMuted,
            ),
          ),
          SizedBox(height: 3.h),
          Text(
            value,
            style: GoogleFonts.barlowCondensed(
              fontSize: 18.sp,
              fontWeight: FontWeight.w900,
              height: 1.0,
              letterSpacing: -0.3,
              color: color,
            ),
          ),
          Text(
            unit,
            style: GoogleFonts.barlowCondensed(
              fontSize: 8.sp,
              fontWeight: FontWeight.w600,
              letterSpacing: 0.8,
              color: AppColors.foregroundMuted.withValues(alpha: 0.60),
            ),
          ),
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

// ── Metrics grid ──────────────────────────────────────────────────────────────
class _MetricsGrid extends StatelessWidget {
  const _MetricsGrid();

  static bool _isToday(DateTime dt) {
    final now = DateTime.now();
    return dt.year == now.year && dt.month == now.month && dt.day == now.day;
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: 16.w),
      child: BlocBuilder<BillsListBloc, BillsListState>(
        builder: (context, billsState) {
          final todaysBills = billsState is BillsListLoaded
              ? billsState.bills.where((b) => _isToday(b.billingDate)).toList()
              : <Bill>[];
          final visitedIds = todaysBills.map((b) => b.outletId).toSet();
          final ordersValue =
              billsState is BillsListLoaded ? todaysBills.length.toString() : '—';

          return BlocBuilder<OutletsBloc, OutletsState>(
            builder: (context, outletsState) {
              final totalOutlets = outletsState is OutletsLoaded
                  ? outletsState.outlets.length
                  : 0;
              final visitedValue = billsState is BillsListLoaded
                  ? visitedIds.length.toString()
                  : '—';
              final progressValue =
                  (billsState is BillsListLoaded && outletsState is OutletsLoaded)
                      ? '${visitedIds.length}/$totalOutlets'
                      : '—';

              return Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: _MetricTile(
                      icon: Icons.receipt_long_rounded,
                      label: 'Orders\nPlaced',
                      value: ordersValue,
                      color: AppColors.primary,
                      large: true,
                    ),
                  ),
                  SizedBox(width: 10.w),
                  Expanded(
                    child: Column(
                      children: [
                        _MetricTile(
                          icon: Icons.storefront_rounded,
                          label: 'Outlets Visited',
                          value: visitedValue,
                          color: AppColors.primary,
                          large: false,
                        ),
                        SizedBox(height: 10.h),
                        _MetricTile(
                          icon: Icons.map_outlined,
                          label: 'Route Progress',
                          value: progressValue,
                          color: AppColors.primary,
                          large: false,
                        ),
                      ],
                    ),
                  ),
                ],
              );
            },
          );
        },
      ),
    );
  }
}

class _MetricTile extends StatelessWidget {
  const _MetricTile({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
    required this.large,
  });
  final IconData icon;
  final String label, value;
  final Color color;
  final bool large;

  @override
  Widget build(BuildContext context) {
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
                          fontSize: 54.sp,
                          fontWeight: FontWeight.w900,
                          height: 1.0,
                          letterSpacing: -2,
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
                      fontSize: 26.sp,
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

// ── Actions grid ──────────────────────────────────────────────────────────────
class _ActionsGrid extends StatelessWidget {
  const _ActionsGrid();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: 16.w),
      child: Column(
        children: [
          Row(
            children: [
              Expanded(
                child: _TileActionCard(
                  icon: Icons.receipt_long_rounded,
                  title: 'MY ORDERS',
                  subtitle: 'View & sync status',
                  color: AppColors.primary,
                  onTap: () => context.push('/sales-rep/bills'),
                ),
              ),
              SizedBox(width: 10.w),
              Expanded(
                child: _TileActionCard(
                  icon: Icons.bar_chart_rounded,
                  title: 'BILLING\nREPORT',
                  subtitle: 'Outlet-wise summary',
                  color: AppColors.primary,
                  onTap: () => context.push('/sales-rep/outlet-billings'),
                ),
              ),
            ],
          ),
          SizedBox(height: 10.h),
          Row(
            children: [
              Expanded(
                child: _TileActionCard(
                  icon: Icons.report_problem_outlined,
                  title: 'NOT BILLING',
                  subtitle: 'Record outlet visit',
                  color: AppColors.primary,
                  onTap: () => context.push('/sales-rep/not-billings'),
                ),
              ),
              SizedBox(width: 10.w),
              Expanded(
                child: _TileActionCard(
                  icon: Icons.map_rounded,
                  title: "TODAY'S\nMAP",
                  subtitle: 'View route on map',
                  color: AppColors.primary,
                  onTap: () => context.push('/sales-rep/todays-route-map'),
                ),
              ),
            ],
          ),
          SizedBox(height: 10.h),
          Row(
            children: [
              Expanded(
                child: _TileActionCard(
                  icon: Icons.storefront_rounded,
                  title: 'ADD OUTLET',
                  subtitle: 'Register new outlet',
                  color: AppColors.primary,
                  onTap: () => context.push('/sales-rep/outlets/create'),
                ),
              ),
              SizedBox(width: 10.w),
              Expanded(
                child: _TileActionCard(
                  icon: Icons.sync_rounded,
                  title: 'SYNC DATA',
                  subtitle: 'Keep device updated',
                  color: AppColors.primary,
                  onTap: () => context.push('/sales-rep/sync'),
                ),
              ),
            ],
          ),
          SizedBox(height: 10.h),
          Row(
            children: [
              Expanded(
                child: _TileActionCard(
                  icon: Icons.assignment_turned_in_rounded,
                  title: 'PURCHASE\nORDERS',
                  subtitle: 'Approve pending orders',
                  color: AppColors.primary,
                  onTap: () => context.push('/sales-rep/purchase-orders'),
                ),
              ),
              SizedBox(width: 10.w),
              const Expanded(child: SizedBox()),
            ],
          ),
          SizedBox(height: 10.h),
          Row(
            children: [
              Expanded(
                child: _SecondaryAction(
                  icon: Icons.location_on_rounded,
                  label: 'Check In',
                  color: AppColors.primary,
                ),
              ),
              SizedBox(width: 10.w),
              Expanded(
                child: _SecondaryAction(
                  icon: Icons.inventory_2_rounded,
                  label: 'Products',
                  color: AppColors.foregroundMuted,
                  onTap: () => context.push('/sales-rep/products'),
                ),
              ),
              SizedBox(width: 10.w),
              Expanded(
                child: _SecondaryAction(
                  icon: Icons.bug_report_rounded,
                  label: 'Debug',
                  color: AppColors.foregroundMuted,
                  onTap: () => context.push('/sales-rep/debug'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

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

class _SecondaryAction extends StatelessWidget {
  const _SecondaryAction({
    required this.icon,
    required this.label,
    required this.color,
    this.onTap,
  });
  final IconData icon;
  final String label;
  final Color color;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12.r),
        child: Ink(
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.07),
            borderRadius: BorderRadius.circular(12.r),
            border: Border.all(color: color.withValues(alpha: 0.15)),
          ),
          padding: EdgeInsets.symmetric(vertical: 14.h),
          child: Column(
            children: [
              Icon(icon, color: color, size: 20.r),
              SizedBox(height: 6.h),
              Text(label,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.2,
                    color: color,
                  )),
            ],
          ),
        ),
      ),
    );
  }
}
