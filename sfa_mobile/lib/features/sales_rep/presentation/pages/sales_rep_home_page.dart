import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:go_router/go_router.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_bloc.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_event.dart';
import 'package:uswatte/features/route_assignment/presentation/bloc/assignments_bloc.dart';

class SalesRepHomePage extends StatefulWidget {
  const SalesRepHomePage({super.key});

  @override
  State<SalesRepHomePage> createState() => _SalesRepHomePageState();
}

class _SalesRepHomePageState extends State<SalesRepHomePage>
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
        vsync: this, duration: const Duration(milliseconds: 1000))
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
      body: CustomScrollView(
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
                  position: _slide(0.15, 0.65), child: const _KpiRow()),
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
                _NavIconBtn(icon: Icons.notifications_outlined, onTap: () {}),
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

    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 0),
      child: Container(
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

// ── KPI row ───────────────────────────────────────────────────────────────────
class _KpiRow extends StatelessWidget {
  const _KpiRow();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 0),
      child: Row(
        children: [
          _KpiCard(label: 'MTD SALES', value: '—', unit: 'LKR',
              color: AppColors.primary),
          SizedBox(width: 10.w),
          _KpiCard(label: 'TARGET', value: '—', unit: 'LKR',
              color: AppColors.primaryMedium),
          SizedBox(width: 10.w),
          _KpiCard(label: 'COVERAGE', value: '—', unit: '%',
              color: AppColors.amber),
        ],
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
  });
  final String label, value, unit;
  final Color color;

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
          ],
        ),
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

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: 16.w),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: _MetricTile(
              icon: Icons.receipt_long_rounded,
              label: 'Orders\nPlaced',
              value: '—',
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
                  value: '—',
                  color: AppColors.primaryMedium,
                  large: false,
                ),
                SizedBox(height: 10.h),
                _MetricTile(
                  icon: Icons.map_outlined,
                  label: 'Route Progress',
                  value: '—',
                  color: AppColors.amber,
                  large: false,
                ),
              ],
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
          _PrimaryAction(
            icon: Icons.add_shopping_cart_rounded,
            title: 'NEW ORDER',
            subtitle: 'Place a customer order',
            onTap: () {},
          ),
          SizedBox(height: 10.h),
          _SyncDataAction(onTap: () => context.push('/sales-rep/sync')),
          SizedBox(height: 10.h),
          Row(
            children: [
              Expanded(
                child: _SecondaryAction(
                  icon: Icons.location_on_rounded,
                  label: 'Check In',
                  color: AppColors.primaryMedium,
                ),
              ),
              SizedBox(width: 10.w),
              Expanded(
                child: _SecondaryAction(
                  icon: Icons.bar_chart_rounded,
                  label: 'My Sales',
                  color: AppColors.foreground,
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
            ],
          ),
        ],
      ),
    );
  }
}

class _SyncDataAction extends StatelessWidget {
  const _SyncDataAction({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12.r),
        child: Ink(
          height: 56.h,
          decoration: BoxDecoration(
            color: AppColors.primary.withValues(alpha: 0.06),
            borderRadius: BorderRadius.circular(12.r),
            border: Border.all(
              color: AppColors.primary.withValues(alpha: 0.35),
              width: 1.5,
            ),
          ),
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: 20.w),
            child: Row(
              children: [
                Container(
                  width: 32.r,
                  height: 32.r,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(8.r),
                  ),
                  child: Icon(Icons.sync_rounded,
                      color: AppColors.primary, size: 16.r),
                ),
                SizedBox(width: 14.w),
                Expanded(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'SYNC DATA',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 15.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.2,
                          height: 1.0,
                          color: AppColors.primary,
                        ),
                      ),
                      Text(
                        'Keep your device data up to date',
                        style: GoogleFonts.barlow(
                          fontSize: 11.sp,
                          color: AppColors.foregroundMuted,
                        ),
                      ),
                    ],
                  ),
                ),
                Icon(Icons.arrow_forward_ios_rounded,
                    color: AppColors.primary.withValues(alpha: 0.6),
                    size: 12.r),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _PrimaryAction extends StatelessWidget {
  const _PrimaryAction({
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
        borderRadius: BorderRadius.circular(12.r),
        child: Ink(
          height: 66.h,
          decoration: BoxDecoration(
            color: AppColors.primary,
            borderRadius: BorderRadius.circular(12.r),
            boxShadow: [
              BoxShadow(
                color: AppColors.primary.withValues(alpha: 0.30),
                blurRadius: 14,
                offset: const Offset(0, 5),
              ),
            ],
          ),
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: 20.w),
            child: Row(
              children: [
                Container(
                  width: 36.r,
                  height: 36.r,
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.18),
                    borderRadius: BorderRadius.circular(8.r),
                  ),
                  child: Icon(icon, color: Colors.white, size: 18.r),
                ),
                SizedBox(width: 14.w),
                Expanded(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(title,
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 17.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 1.2,
                            height: 1.0,
                            color: Colors.white,
                          )),
                      Text(subtitle,
                          style: GoogleFonts.barlow(
                            fontSize: 11.sp,
                            color: Colors.white.withValues(alpha: 0.7),
                          )),
                    ],
                  ),
                ),
                Icon(Icons.arrow_forward_ios_rounded,
                    color: Colors.white.withValues(alpha: 0.7), size: 13.r),
              ],
            ),
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
