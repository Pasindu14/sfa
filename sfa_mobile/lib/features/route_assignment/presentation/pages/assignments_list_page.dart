import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/route_assignment/domain/entities/daily_route_assignment.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/delete_assignment_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_assignments_usecase.dart';
import 'package:uswatte/features/route_assignment/presentation/bloc/assignments_bloc.dart';

class AssignmentsListPage extends StatelessWidget {
  const AssignmentsListPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => AssignmentsBloc(
        getAssignments: getIt<GetAssignmentsUseCase>(),
        deleteAssignment: getIt<DeleteAssignmentUseCase>(),
      )..add(LoadAssignmentsRequested(date: _today())),
      child: const _AssignmentsView(),
    );
  }

  static DateTime _today() {
    final now = DateTime.now();
    return DateTime(now.year, now.month, now.day);
  }
}

class _AssignmentsView extends StatelessWidget {
  const _AssignmentsView();

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: AppColors.background,
      body: Column(
        children: [
          const _Header(),
          Expanded(
            child: BlocConsumer<AssignmentsBloc, AssignmentsState>(
              listenWhen: (_, s) =>
                  s is AssignmentsLoaded && s.deleteError != null,
              listener: (context, state) {
                if (state is AssignmentsLoaded && state.deleteError != null) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(
                      content: Text(state.deleteError!),
                      backgroundColor: Colors.red.shade700,
                    ),
                  );
                }
              },
              builder: (context, state) {
                if (state is AssignmentsLoading) {
                  return const Center(
                    child: CircularProgressIndicator(
                        color: AppColors.primary, strokeWidth: 2),
                  );
                }
                if (state is AssignmentsError) {
                  return _ErrorBody(
                    message: state.message,
                    date: state.date,
                  );
                }
                if (state is AssignmentsLoaded) {
                  return _LoadedBody(state: state);
                }
                return const SizedBox.shrink();
              },
            ),
          ),
        ],
      ),
    );
  }
}

// ── Header ────────────────────────────────────────────────────────────────────
class _Header extends StatelessWidget {
  const _Header();

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<AssignmentsBloc, AssignmentsState>(
      builder: (context, state) {
        final date = switch (state) {
          AssignmentsLoading s => s.date,
          AssignmentsLoaded s => s.date,
          AssignmentsError s => s.date,
          _ => null,
        };
        final selectedDate = date ?? _today();

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
              padding: EdgeInsets.fromLTRB(8.w, 4.h, 8.w, 16.h),
              child: Column(
                children: [
                  // Back + title row
                  Row(
                    children: [
                      _IconBtn(
                        icon: Icons.arrow_back_ios_new_rounded,
                        onTap: () => context.pop(),
                      ),
                      SizedBox(width: 6.w),
                      Expanded(
                        child: Text(
                          'ROUTE ASSIGNMENTS',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 18.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 1.5,
                            color: Colors.white,
                          ),
                        ),
                      ),
                    ],
                  ),
                  SizedBox(height: 14.h),
                  // Date navigator
                  Container(
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(10.r),
                    ),
                    padding:
                        EdgeInsets.symmetric(horizontal: 4.w, vertical: 4.h),
                    child: Row(
                      children: [
                        _NavArrow(
                          icon: Icons.chevron_left_rounded,
                          onTap: () {
                            final prev = selectedDate
                                .subtract(const Duration(days: 1));
                            context
                                .read<AssignmentsBloc>()
                                .add(DateChanged(prev));
                          },
                        ),
                        Expanded(
                          child: GestureDetector(
                            onTap: () async {
                              final picked = await showDatePicker(
                                context: context,
                                initialDate: selectedDate,
                                firstDate: DateTime(2020),
                                lastDate: DateTime(2030),
                                builder: (ctx, child) => Theme(
                                  data: Theme.of(ctx).copyWith(
                                    colorScheme: ColorScheme.light(
                                      primary: AppColors.primary,
                                      onPrimary: Colors.white,
                                    ),
                                  ),
                                  child: child!,
                                ),
                              );
                              if (picked != null && context.mounted) {
                                context.read<AssignmentsBloc>().add(DateChanged(
                                    DateTime(
                                        picked.year,
                                        picked.month,
                                        picked.day)));
                              }
                            },
                            child: Column(
                              children: [
                                Text(
                                  _dateLabel(selectedDate),
                                  textAlign: TextAlign.center,
                                  style: GoogleFonts.barlowCondensed(
                                    fontSize: 18.sp,
                                    fontWeight: FontWeight.w700,
                                    letterSpacing: 0.5,
                                    color: Colors.white,
                                  ),
                                ),
                                if (_isToday(selectedDate))
                                  Text(
                                    'TODAY',
                                    style: GoogleFonts.barlowCondensed(
                                      fontSize: 9.sp,
                                      fontWeight: FontWeight.w600,
                                      letterSpacing: 2,
                                      color:
                                          Colors.white.withValues(alpha: 0.65),
                                    ),
                                  ),
                              ],
                            ),
                          ),
                        ),
                        _NavArrow(
                          icon: Icons.chevron_right_rounded,
                          onTap: () {
                            final next =
                                selectedDate.add(const Duration(days: 1));
                            context
                                .read<AssignmentsBloc>()
                                .add(DateChanged(next));
                          },
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  DateTime _today() {
    final now = DateTime.now();
    return DateTime(now.year, now.month, now.day);
  }

  bool _isToday(DateTime date) {
    final now = DateTime.now();
    return date.year == now.year &&
        date.month == now.month &&
        date.day == now.day;
  }

  String _dateLabel(DateTime date) {
    const months = [
      'JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN',
      'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC'
    ];
    return '${months[date.month - 1]} ${date.day}, ${date.year}';
  }
}

class _IconBtn extends StatelessWidget {
  const _IconBtn({required this.icon, required this.onTap});
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 36.r,
        height: 36.r,
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: 0.15),
          borderRadius: BorderRadius.circular(8.r),
        ),
        child: Icon(icon, color: Colors.white, size: 18.r),
      ),
    );
  }
}

class _NavArrow extends StatelessWidget {
  const _NavArrow({required this.icon, required this.onTap});
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 36.r,
        height: 36.r,
        alignment: Alignment.center,
        child: Icon(icon,
            color: Colors.white.withValues(alpha: 0.9), size: 22.r),
      ),
    );
  }
}

// ── Loaded body ───────────────────────────────────────────────────────────────
class _LoadedBody extends StatelessWidget {
  const _LoadedBody({required this.state});
  final AssignmentsLoaded state;

  @override
  Widget build(BuildContext context) {
    if (state.assignments.isEmpty) {
      return _EmptyState(date: state.date);
    }

    return RefreshIndicator(
      color: AppColors.primary,
      onRefresh: () async {
        context
            .read<AssignmentsBloc>()
            .add(LoadAssignmentsRequested(date: state.date));
        await context.read<AssignmentsBloc>().stream.firstWhere(
              (s) => s is AssignmentsLoaded || s is AssignmentsError,
            );
      },
      child: ListView.builder(
        padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 24.h),
        itemCount: state.assignments.length + 1,
        itemBuilder: (context, index) {
          if (index == 0) {
            return Padding(
              padding: EdgeInsets.only(bottom: 12.h),
              child: Row(
                children: [
                  Container(
                    padding: EdgeInsets.symmetric(
                        horizontal: 10.w, vertical: 5.h),
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(
                        colors: [AppColors.primaryLight, AppColors.primaryDark],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      ),
                      borderRadius: BorderRadius.circular(20.r),
                    ),
                    child: Text(
                      '${state.totalCount}',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 13.sp,
                        fontWeight: FontWeight.w800,
                        color: Colors.white,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ),
                  SizedBox(width: 8.w),
                  Text(
                    state.totalCount == 1
                        ? 'assignment scheduled'
                        : 'assignments scheduled',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w500,
                      color: AppColors.foregroundMuted,
                      letterSpacing: 0.3,
                    ),
                  ),
                ],
              ),
            );
          }
          final item = state.assignments[index - 1];
          final isRequesting = state.requestingId == item.id;
          return _AssignmentCard(
            assignment: item,
            isRequesting: isRequesting,
            onDelete: () => _requestCancellation(context, item),
          );
        },
      ),
    );
  }

  void _requestCancellation(BuildContext context, DailyRouteAssignment item) {
    final reasonController = TextEditingController();
    showDialog<String?>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape:
            RoundedRectangleBorder(borderRadius: BorderRadius.circular(14.r)),
        title: Text(
          'Request Cancellation',
          style: GoogleFonts.barlowCondensed(
            fontSize: 18.sp,
            fontWeight: FontWeight.w700,
          ),
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Send a cancellation request to your manager for ${item.userName}\'s route assignment?',
              style: GoogleFonts.barlow(fontSize: 14.sp),
            ),
            SizedBox(height: 14.h),
            TextField(
              controller: reasonController,
              maxLines: 2,
              decoration: InputDecoration(
                hintText: 'Reason (e.g. route flooded, road blocked)',
                hintStyle: GoogleFonts.barlow(
                  fontSize: 13.sp,
                  color: AppColors.foregroundMuted,
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8.r),
                  borderSide: BorderSide(color: AppColors.surfaceVariant),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8.r),
                  borderSide: BorderSide(color: AppColors.primary),
                ),
                contentPadding:
                    EdgeInsets.symmetric(horizontal: 12.w, vertical: 10.h),
              ),
              style: GoogleFonts.barlow(fontSize: 13.sp),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(null),
            child: Text('Cancel',
                style: GoogleFonts.barlow(color: AppColors.foregroundMuted)),
          ),
          TextButton(
            onPressed: () =>
                Navigator.of(ctx).pop(reasonController.text.trim()),
            child: Text(
              'Send Request',
              style: GoogleFonts.barlow(
                  color: AppColors.primary, fontWeight: FontWeight.w600),
            ),
          ),
        ],
      ),
    ).then((reason) {
      if (reason != null && context.mounted) {
        context.read<AssignmentsBloc>().add(
              DeleteAssignmentRequested(
                item.id,
                reason: reason.isNotEmpty ? reason : null,
              ),
            );
      }
    });
  }
}

// ── Assignment card ────────────────────────────────────────────────────────────
class _AssignmentCard extends StatelessWidget {
  const _AssignmentCard({
    required this.assignment,
    required this.isRequesting,
    required this.onDelete,
  });

  final DailyRouteAssignment assignment;
  final bool isRequesting;
  final VoidCallback onDelete;

  String _initials(String name) {
    final parts = name.trim().split(RegExp(r'\s+'));
    if (parts.length >= 2) {
      return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    }
    return name.isNotEmpty ? name[0].toUpperCase() : '?';
  }

  String _timeAgo(DateTime dt) {
    final diff = DateTime.now().difference(dt);
    if (diff.inMinutes < 1) return 'just now';
    if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
    if (diff.inHours < 24) return '${diff.inHours}h ago';
    return '${diff.inDays}d ago';
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(bottom: 10.h),
      child: Container(
        decoration: BoxDecoration(
          color: const Color(0xFFFAF9F5),
          borderRadius: BorderRadius.circular(14.r),
          border: Border.all(color: const Color(0xFFEDEBE3), width: 1),
          boxShadow: [
            BoxShadow(
              color: AppColors.primaryDark.withValues(alpha: 0.07),
              blurRadius: 14,
              offset: const Offset(0, 3),
            ),
          ],
        ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(14.r),
          child: Stack(
            children: [
              // Left accent stripe
              Positioned(
                left: 0,
                top: 0,
                bottom: 0,
                child: Container(
                  width: 4,
                  decoration: const BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topCenter,
                      end: Alignment.bottomCenter,
                      colors: [AppColors.primaryLight, AppColors.primaryDark],
                    ),
                  ),
                ),
              ),
              // Card content
              Padding(
                padding:
                    EdgeInsets.fromLTRB(18.w, 12.h, 12.w, 12.h),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        // Gradient avatar
                        Container(
                          width: 44.r,
                          height: 44.r,
                          decoration: BoxDecoration(
                            gradient: const LinearGradient(
                              begin: Alignment.topLeft,
                              end: Alignment.bottomRight,
                              colors: [
                                AppColors.primaryLight,
                                AppColors.primaryDark,
                              ],
                            ),
                            borderRadius: BorderRadius.circular(12.r),
                          ),
                          child: Center(
                            child: Text(
                              _initials(assignment.userName),
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 16.sp,
                                fontWeight: FontWeight.w800,
                                color: Colors.white,
                                letterSpacing: 0.5,
                              ),
                            ),
                          ),
                        ),
                        SizedBox(width: 12.w),
                        // Name + route badge
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                assignment.userName,
                                style: GoogleFonts.barlowCondensed(
                                  fontSize: 16.sp,
                                  fontWeight: FontWeight.w700,
                                  color: AppColors.foreground,
                                  letterSpacing: 0.2,
                                ),
                              ),
                              SizedBox(height: 5.h),
                              _RouteBadge(routeName: assignment.routeName),
                            ],
                          ),
                        ),
                        SizedBox(width: 8.w),
                        // Action button — changes based on deletion status
                        if (isRequesting)
                          Padding(
                            padding: EdgeInsets.all(8.r),
                            child: SizedBox(
                              width: 18.r,
                              height: 18.r,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: AppColors.primary,
                              ),
                            ),
                          )
                        else if (assignment.deletionStatus ==
                            DeletionStatus.pendingApproval)
                          _PendingButton()
                        else
                          _DeleteButton(onTap: onDelete),
                      ],
                    ),
                    // Deletion status badge row
                    if (assignment.deletionStatus ==
                        DeletionStatus.pendingApproval) ...[
                      SizedBox(height: 8.h),
                      _DeletionStatusBadge(
                        label: 'PENDING APPROVAL',
                        reason: assignment.deletionRequestReason,
                        color: const Color(0xFFF59E0B),
                        icon: Icons.hourglass_top_rounded,
                      ),
                    ] else if (assignment.deletionStatus ==
                        DeletionStatus.rejected) ...[
                      SizedBox(height: 8.h),
                      _DeletionStatusBadge(
                        label: 'REJECTED',
                        reason: assignment.deletionRejectionReason,
                        color: AppColors.error,
                        icon: Icons.cancel_outlined,
                      ),
                    ],
                    SizedBox(height: 10.h),
                    // Bottom metadata row
                    Row(
                      children: [
                        Container(
                          padding: EdgeInsets.symmetric(
                              horizontal: 7.w, vertical: 2.5.h),
                          decoration: BoxDecoration(
                            color: AppColors.surfaceVariant,
                            borderRadius: BorderRadius.circular(4.r),
                          ),
                          child: Text(
                            '#${assignment.id}',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 10.sp,
                              fontWeight: FontWeight.w600,
                              letterSpacing: 0.8,
                              color: AppColors.foregroundMuted,
                            ),
                          ),
                        ),
                        const Spacer(),
                        Icon(
                          Icons.access_time_rounded,
                          size: 10.r,
                          color: AppColors.foregroundMuted,
                        ),
                        SizedBox(width: 3.w),
                        Text(
                          _timeAgo(assignment.createdAt),
                          style: GoogleFonts.barlow(
                            fontSize: 10.sp,
                            color: AppColors.foregroundMuted,
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
      ),
    );
  }
}

class _RouteBadge extends StatelessWidget {
  const _RouteBadge({required this.routeName});
  final String routeName;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
      decoration: BoxDecoration(
        color: AppColors.primary.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(5.r),
        border: Border.all(
          color: AppColors.primary.withValues(alpha: 0.18),
          width: 1,
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.route_rounded, size: 10.r, color: AppColors.primary),
          SizedBox(width: 4.w),
          Flexible(
            child: Text(
              routeName,
              style: GoogleFonts.barlowCondensed(
                fontSize: 11.sp,
                fontWeight: FontWeight.w600,
                color: AppColors.primaryMedium,
                letterSpacing: 0.3,
              ),
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
      ),
    );
  }
}

class _DeleteButton extends StatelessWidget {
  const _DeleteButton({required this.onTap});
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 34.r,
        height: 34.r,
        decoration: BoxDecoration(
          color: AppColors.error.withValues(alpha: 0.06),
          borderRadius: BorderRadius.circular(9.r),
          border: Border.all(
            color: AppColors.error.withValues(alpha: 0.15),
            width: 1,
          ),
        ),
        child: Icon(
          Icons.delete_outline_rounded,
          size: 16.r,
          color: AppColors.error,
        ),
      ),
    );
  }
}

class _PendingButton extends StatelessWidget {
  const _PendingButton();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 34.r,
      height: 34.r,
      decoration: BoxDecoration(
        color: const Color(0xFFF59E0B).withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(9.r),
        border: Border.all(
          color: const Color(0xFFF59E0B).withValues(alpha: 0.3),
          width: 1,
        ),
      ),
      child: Icon(
        Icons.hourglass_top_rounded,
        size: 16.r,
        color: const Color(0xFFF59E0B),
      ),
    );
  }
}

class _DeletionStatusBadge extends StatelessWidget {
  const _DeletionStatusBadge({
    required this.label,
    required this.color,
    required this.icon,
    this.reason,
  });
  final String label;
  final Color color;
  final IconData icon;
  final String? reason;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 6.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.07),
        borderRadius: BorderRadius.circular(7.r),
        border: Border.all(color: color.withValues(alpha: 0.2)),
      ),
      child: Row(
        children: [
          Icon(icon, size: 12.r, color: color),
          SizedBox(width: 6.w),
          Text(
            label,
            style: GoogleFonts.barlowCondensed(
              fontSize: 11.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.2,
              color: color,
            ),
          ),
          if (reason != null && reason!.isNotEmpty) ...[
            SizedBox(width: 6.w),
            Expanded(
              child: Text(
                '· $reason',
                style: GoogleFonts.barlow(
                  fontSize: 11.sp,
                  color: color.withValues(alpha: 0.8),
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

// ── Empty state ───────────────────────────────────────────────────────────────
class _EmptyState extends StatelessWidget {
  const _EmptyState({this.date});
  final DateTime? date;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 64.r,
              height: 64.r,
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.08),
                shape: BoxShape.circle,
              ),
              child: Icon(Icons.event_busy_rounded,
                  size: 30.r, color: AppColors.primary),
            ),
            SizedBox(height: 16.h),
            Text(
              'No assignments',
              style: GoogleFonts.barlowCondensed(
                fontSize: 20.sp,
                fontWeight: FontWeight.w700,
                color: AppColors.foreground,
              ),
            ),
            SizedBox(height: 6.h),
            Text(
              'No routes have been assigned for this day yet.',
              textAlign: TextAlign.center,
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
}

// ── Error body ────────────────────────────────────────────────────────────────
class _ErrorBody extends StatelessWidget {
  const _ErrorBody({required this.message, this.date});
  final String message;
  final DateTime? date;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.wifi_off_rounded,
                size: 36.r, color: AppColors.foregroundMuted),
            SizedBox(height: 14.h),
            Text(
              message,
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(
                fontSize: 14.sp,
                color: AppColors.foregroundMuted,
              ),
            ),
            SizedBox(height: 16.h),
            TextButton.icon(
              onPressed: () => context
                  .read<AssignmentsBloc>()
                  .add(LoadAssignmentsRequested(date: date)),
              icon: const Icon(Icons.refresh_rounded),
              label: const Text('Retry'),
              style: TextButton.styleFrom(foregroundColor: AppColors.primary),
            ),
          ],
        ),
      ),
    );
  }
}
