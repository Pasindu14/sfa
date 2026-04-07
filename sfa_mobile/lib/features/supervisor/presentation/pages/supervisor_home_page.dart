import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';

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
              opacity: _fade(0.25, 0.75),
              child: SlideTransition(
                position: _slide(0.25, 0.75),
                child: _SectionLabel('QUICK ACTIONS'),
              ),
            ),
          ),
          SliverToBoxAdapter(
            child: FadeTransition(
              opacity: _fade(0.35, 0.85),
              child: SlideTransition(
                position: _slide(0.35, 0.85),
                child: const _ActionsSection(),
              ),
            ),
          ),
          SliverToBoxAdapter(child: SizedBox(height: 40.h)),
        ],
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
      child: _PrimaryAction(
        icon: Icons.map_rounded,
        title: 'ASSIGN DAILY ROUTE',
        subtitle: 'Schedule a route for a sales rep',
        onTap: () => context.go('/supervisor/assign-route'),
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
