import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_request.dart';
import 'package:uswatte/features/route_unlock/presentation/cubit/unlock_requests_cubit.dart';
import 'package:uswatte/features/route_unlock/presentation/widgets/unlock_request_detail_sheet.dart';
import 'package:uswatte/features/route_unlock/presentation/widgets/unlock_status_style.dart';

/// Supervisor queue of today's route unlock requests from their direct reports.
class UnlockRequestsPage extends StatelessWidget {
  const UnlockRequestsPage({super.key});

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: const Color(0xFFF8F7F5),
      body: Column(
        children: [
          const _Header(),
          Expanded(
            child: BlocBuilder<UnlockRequestsCubit, UnlockRequestsState>(
              builder: (context, state) {
                final cubit = context.read<UnlockRequestsCubit>();
                if (!state.loaded && state.error == null) {
                  return const Center(
                    child: CircularProgressIndicator(color: AppColors.primary),
                  );
                }
                if (!state.loaded) {
                  return _ErrorView(message: state.error!, onRetry: cubit.load);
                }

                return RefreshIndicator(
                  color: AppColors.primary,
                  backgroundColor: Colors.white,
                  onRefresh: cubit.load,
                  child: ListView(
                    // Pull-to-refresh must work on a short (or empty) list too.
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: EdgeInsets.fromLTRB(16.w, 4.h, 16.w, 40.h),
                    children: [
                      _SectionTitle(
                        'PENDING',
                        count: state.pending.length,
                        color: AppColors.warning,
                      ),
                      if (state.pending.isEmpty)
                        const _EmptyLine('No requests waiting for you.')
                      else
                        for (final r in state.pending)
                          _RequestCard(
                            request: r,
                            onTap: () => _open(context, r),
                          ),
                      _SectionTitle(
                        'TODAY\'S DECISIONS',
                        count: state.decided.length,
                        color: AppColors.foregroundMuted,
                      ),
                      if (state.decided.isEmpty)
                        const _EmptyLine('Nothing decided yet today.')
                      else
                        for (final r in state.decided)
                          _RequestCard(
                            request: r,
                            onTap: () => _open(context, r),
                          ),
                    ],
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _open(BuildContext context, RouteUnlockRequest r) async {
    final messenger = ScaffoldMessenger.of(context);
    final message = await showUnlockRequestDetailSheet(
      context,
      cubit: context.read<UnlockRequestsCubit>(),
      request: r,
    );
    if (message == null) return;
    messenger.showSnackBar(SnackBar(
      content: Text(
        message,
        style: GoogleFonts.barlow(
            color: Colors.white, fontWeight: FontWeight.w500),
      ),
      behavior: SnackBarBehavior.floating,
      margin: EdgeInsets.all(16.w),
      shape:
          RoundedRectangleBorder(borderRadius: BorderRadius.circular(8.r)),
      duration: const Duration(seconds: 3),
    ));
  }
}

class _Header extends StatelessWidget {
  const _Header();

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
        child: Padding(
          padding: EdgeInsets.fromLTRB(8.w, 4.h, 16.w, 18.h),
          child: Row(
            children: [
              GestureDetector(
                onTap: () => context.canPop()
                    ? context.pop()
                    : context.goNamed('supervisorHome'),
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
              SizedBox(width: 6.w),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      'UNLOCK REQUESTS',
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
                      'Reps asking to bill their whole route today',
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: Colors.white.withValues(alpha: 0.70),
                      ),
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

class _SectionTitle extends StatelessWidget {
  final String text;
  final int count;
  final Color color;

  const _SectionTitle(this.text, {required this.count, required this.color});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(2.w, 18.h, 2.w, 10.h),
      child: Row(
        children: [
          Container(
            width: 3.w,
            height: 13.h,
            decoration: BoxDecoration(
              color: color,
              borderRadius: BorderRadius.circular(2.r),
            ),
          ),
          SizedBox(width: 8.w),
          Text(
            '$text  ·  $count',
            style: GoogleFonts.barlowCondensed(
              fontSize: 11.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 2.5,
              color: AppColors.foregroundMuted,
            ),
          ),
        ],
      ),
    );
  }
}

class _EmptyLine extends StatelessWidget {
  final String text;
  const _EmptyLine(this.text);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 10.h, horizontal: 4.w),
      child: Text(
        text,
        style: GoogleFonts.barlow(
            fontSize: 13.sp, color: AppColors.foregroundMuted),
      ),
    );
  }
}

class _RequestCard extends StatelessWidget {
  final RouteUnlockRequest request;
  final VoidCallback onTap;

  const _RequestCard({required this.request, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final r = request;
    final style = unlockStatusStyle(r);

    return Padding(
      padding: EdgeInsets.only(bottom: 10.h),
      child: Material(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14.r),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(14.r),
          child: Container(
            padding: EdgeInsets.all(14.r),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(14.r),
              border: Border.all(color: AppColors.surfaceVariant),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        r.userName ?? r.loginName ?? 'Rep #${r.userId}',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 16.sp,
                          fontWeight: FontWeight.w800,
                          color: AppColors.foreground,
                        ),
                      ),
                    ),
                    UnlockStatusPill(style: style),
                  ],
                ),
                SizedBox(height: 2.h),
                Text(
                  '${r.routeName ?? 'Route #${r.routeId}'}  ·  '
                  'asked at ${formatUnlockTime(r.requestedAt)}',
                  style: GoogleFonts.barlow(
                      fontSize: 12.sp, color: AppColors.foregroundMuted),
                ),
                SizedBox(height: 8.h),
                Text(
                  '"${r.requestReason}"',
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.barlow(
                    fontSize: 13.sp,
                    height: 1.35,
                    color: AppColors.foreground,
                  ),
                ),
                SizedBox(height: 6.h),
                Row(
                  children: [
                    Icon(Icons.my_location_rounded,
                        size: 12.r, color: AppColors.foregroundMuted),
                    SizedBox(width: 4.w),
                    Text(
                      describeUnlockLocation(r),
                      style: GoogleFonts.barlow(
                          fontSize: 11.sp, color: AppColors.foregroundMuted),
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

class _ErrorView extends StatelessWidget {
  final String message;
  final Future<void> Function() onRetry;

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
                size: 40.r, color: AppColors.foregroundMuted),
            SizedBox(height: 12.h),
            Text(
              message,
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(
                  fontSize: 13.sp, color: AppColors.foregroundMuted),
            ),
            SizedBox(height: 12.h),
            TextButton(onPressed: onRetry, child: const Text('Retry')),
          ],
        ),
      ),
    );
  }
}
