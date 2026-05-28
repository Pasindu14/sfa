import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/notifications/domain/entities/notification_entity.dart';
import '../bloc/notifications_bloc.dart';

class NotificationsPage extends StatefulWidget {
  const NotificationsPage({super.key});

  @override
  State<NotificationsPage> createState() => _NotificationsPageState();
}

class _NotificationsPageState extends State<NotificationsPage>
    with SingleTickerProviderStateMixin {
  late AnimationController _ctrl;

  Animation<double> _fade(double from, double to) => CurvedAnimation(
      parent: _ctrl, curve: Interval(from, to, curve: Curves.easeOut));

  Animation<Offset> _slide(double from, double to) =>
      Tween<Offset>(begin: const Offset(0, 0.06), end: Offset.zero).animate(
          CurvedAnimation(
              parent: _ctrl,
              curve: Interval(from, to, curve: Curves.easeOutCubic)));

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
        vsync: this, duration: const Duration(milliseconds: 750))
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
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: const Color(0xFFF8F7F5),
      body: Column(
        children: [
          BlocBuilder<NotificationsBloc, NotificationsState>(
            builder: (context, state) {
              final unread = state is NotificationsLoaded ? state.unreadCount : 0;
              return _Header(unreadCount: unread, onMarkAll: () {
                context.read<NotificationsBloc>().add(const MarkAllNotificationsRead());
              });
            },
          ),
          Expanded(
            child: BlocBuilder<NotificationsBloc, NotificationsState>(
              builder: (context, state) {
                if (state is NotificationsLoading) {
                  return const Center(child: AppSpinner());
                }

                if (state is NotificationsError) {
                  return Center(
                    child: Padding(
                      padding: EdgeInsets.all(24.r),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Container(
                            width: 56.r,
                            height: 56.r,
                            decoration: BoxDecoration(
                              color: AppColors.error.withValues(alpha: 0.08),
                              borderRadius: BorderRadius.circular(14.r),
                            ),
                            child: Icon(Icons.error_outline_rounded,
                                size: 26.r, color: AppColors.error),
                          ),
                          SizedBox(height: 14.h),
                          Text(
                            state.message,
                            textAlign: TextAlign.center,
                            style: GoogleFonts.barlow(
                                fontSize: 14.sp,
                                color: AppColors.foregroundMuted),
                          ),
                          SizedBox(height: 18.h),
                          SizedBox(
                            width: 140.w,
                            child: ElevatedButton(
                              onPressed: () => context
                                  .read<NotificationsBloc>()
                                  .add(const LoadNotifications()),
                              child: Text('Retry',
                                  style: GoogleFonts.barlowCondensed(
                                    fontSize: 14.sp,
                                    fontWeight: FontWeight.w700,
                                    letterSpacing: 1.5,
                                  )),
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                }

                final items = switch (state) {
                  NotificationsLoaded(items: final i) => i,
                  NotificationsLoadingMore(items: final i) => i,
                  _ => <NotificationEntity>[],
                };

                if (items.isEmpty) {
                  return FadeTransition(
                    opacity: _fade(0.0, 0.6),
                    child: Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Container(
                            width: 72.r,
                            height: 72.r,
                            decoration: BoxDecoration(
                              color: AppColors.surface,
                              borderRadius: BorderRadius.circular(20.r),
                              border: Border.all(color: AppColors.surfaceVariant),
                            ),
                            child: Icon(Icons.notifications_none_outlined,
                                size: 32.r, color: AppColors.foregroundMuted),
                          ),
                          SizedBox(height: 16.h),
                          Text(
                            'No notifications yet',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 18.sp,
                              fontWeight: FontWeight.w600,
                              color: AppColors.foregroundMuted,
                            ),
                          ),
                          SizedBox(height: 4.h),
                          Text(
                            "You're all caught up",
                            style: GoogleFonts.barlow(
                              fontSize: 13.sp,
                              color: AppColors.foregroundMuted
                                  .withValues(alpha: 0.6),
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                }

                final today = <NotificationEntity>[];
                final earlier = <NotificationEntity>[];
                final now = DateTime.now();
                for (final n in items) {
                  final diff = now.difference(n.createdAt);
                  if (diff.inHours < 24 && n.createdAt.day == now.day) {
                    today.add(n);
                  } else {
                    earlier.add(n);
                  }
                }

                return RefreshIndicator(
                  color: AppColors.primary,
                  backgroundColor: Colors.white,
                  onRefresh: () async {
                    context
                        .read<NotificationsBloc>()
                        .add(const LoadNotifications());
                    await context.read<NotificationsBloc>().stream.firstWhere(
                        (s) =>
                            s is NotificationsLoaded ||
                            s is NotificationsError);
                  },
                  child: NotificationListener<ScrollNotification>(
                    onNotification: (notification) {
                      if (notification is ScrollEndNotification &&
                          notification.metrics.pixels >=
                              notification.metrics.maxScrollExtent - 200) {
                        context
                            .read<NotificationsBloc>()
                            .add(const LoadMoreNotifications());
                      }
                      return false;
                    },
                    child: CustomScrollView(
                      slivers: [
                        SliverToBoxAdapter(child: SizedBox(height: 8.h)),
                        if (today.isNotEmpty) ...[
                          SliverToBoxAdapter(
                            child: FadeTransition(
                              opacity: _fade(0.0, 0.4),
                              child: const _SectionLabel('TODAY'),
                            ),
                          ),
                          SliverList(
                            delegate: SliverChildBuilderDelegate(
                              (context, index) {
                                final delay = 0.05 + index * 0.04;
                                return FadeTransition(
                                  opacity: _fade(delay.clamp(0, 0.9),
                                      (delay + 0.3).clamp(0, 1.0)),
                                  child: SlideTransition(
                                    position: _slide(delay.clamp(0, 0.9),
                                        (delay + 0.3).clamp(0, 1.0)),
                                    child: _NotificationTile(
                                        notification: today[index]),
                                  ),
                                );
                              },
                              childCount: today.length,
                            ),
                          ),
                        ],
                        if (earlier.isNotEmpty) ...[
                          SliverToBoxAdapter(
                            child: FadeTransition(
                              opacity: _fade(0.2, 0.6),
                              child: const _SectionLabel('EARLIER'),
                            ),
                          ),
                          SliverList(
                            delegate: SliverChildBuilderDelegate(
                              (context, index) {
                                final delay = 0.25 + index * 0.04;
                                return FadeTransition(
                                  opacity: _fade(delay.clamp(0, 0.9),
                                      (delay + 0.3).clamp(0, 1.0)),
                                  child: SlideTransition(
                                    position: _slide(delay.clamp(0, 0.9),
                                        (delay + 0.3).clamp(0, 1.0)),
                                    child: _NotificationTile(
                                        notification: earlier[index]),
                                  ),
                                );
                              },
                              childCount: earlier.length,
                            ),
                          ),
                        ],
                        if (state is NotificationsLoadingMore)
                          SliverToBoxAdapter(
                            child: Padding(
                              padding: EdgeInsets.all(20.r),
                              child: const Center(child: AppSpinner.small()),
                            ),
                          ),
                        SliverToBoxAdapter(child: SizedBox(height: 24.h)),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

// ── Gradient header ───────────────────────────────────────────────────────────
class _Header extends StatelessWidget {
  final int unreadCount;
  final VoidCallback onMarkAll;
  const _Header({required this.unreadCount, required this.onMarkAll});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [
            Color(0xFF7C2D12),
            AppColors.primaryDark,
            AppColors.primary,
          ],
        ),
      ),
      child: Stack(
        children: [
          // Decorative circles
          Positioned(
            right: -20.w,
            top: -20.h,
            child: Container(
              width: 130.r,
              height: 130.r,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: Colors.white.withValues(alpha: 0.05),
              ),
            ),
          ),
          Positioned(
            right: 60.w,
            bottom: -10.h,
            child: Container(
              width: 60.r,
              height: 60.r,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: Colors.white.withValues(alpha: 0.04),
              ),
            ),
          ),
          SafeArea(
            bottom: false,
            child: Padding(
              padding: EdgeInsets.fromLTRB(8.w, 4.h, 16.w, 18.h),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  // Back button
                  GestureDetector(
                    onTap: () => Navigator.of(context).pop(),
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
                  // Title block
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          'NOTIFICATIONS',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 22.sp,
                            fontWeight: FontWeight.w900,
                            letterSpacing: 1.5,
                            height: 1.0,
                            color: Colors.white,
                          ),
                        ),
                        SizedBox(height: 2.h),
                        Text(
                          unreadCount > 0
                              ? '$unreadCount unread'
                              : 'All caught up',
                          style: GoogleFonts.barlow(
                            fontSize: 11.sp,
                            color: Colors.white.withValues(alpha: 0.65),
                          ),
                        ),
                      ],
                    ),
                  ),
                  // Mark all read button or icon
                  if (unreadCount > 0)
                    GestureDetector(
                      onTap: onMarkAll,
                      child: Container(
                        padding: EdgeInsets.symmetric(
                            horizontal: 12.w, vertical: 8.h),
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(8.r),
                          border: Border.all(
                              color: Colors.white.withValues(alpha: 0.25)),
                        ),
                        child: Text(
                          'Mark all read',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 12.sp,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 0.3,
                            color: Colors.white,
                          ),
                        ),
                      ),
                    )
                  else
                    Container(
                      width: 40.r,
                      height: 40.r,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(10.r),
                        border: Border.all(
                            color: Colors.white.withValues(alpha: 0.2)),
                      ),
                      child: Icon(Icons.notifications_rounded,
                          size: 20.r, color: Colors.white),
                    ),
                ],
              ),
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
      padding: EdgeInsets.fromLTRB(20.w, 16.h, 20.w, 8.h),
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
          Text(
            text,
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

// ── Notification tile ─────────────────────────────────────────────────────────
class _NotificationTile extends StatelessWidget {
  final NotificationEntity notification;
  const _NotificationTile({required this.notification});

  String _relativeTime(DateTime createdAt) {
    final diff = DateTime.now().difference(createdAt);
    if (diff.inMinutes < 1) return 'Just now';
    if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
    if (diff.inHours < 24) return '${diff.inHours}h ago';
    if (diff.inDays < 7) return '${diff.inDays}d ago';
    return '${createdAt.day}/${createdAt.month}/${createdAt.year}';
  }

  IconData _iconForTitle(String title) {
    final t = title.toLowerCase();
    if (t.contains('purchase') || t.contains('order')) {
      return Icons.receipt_long_rounded;
    }
    if (t.contains('bill')) return Icons.receipt_rounded;
    if (t.contains('route') || t.contains('assign')) return Icons.map_rounded;
    if (t.contains('sync')) return Icons.sync_rounded;
    if (t.contains('payment')) return Icons.payments_rounded;
    return Icons.notifications_rounded;
  }

  @override
  Widget build(BuildContext context) {
    final isRead = notification.isRead;
    final iconData = _iconForTitle(notification.title);

    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 0, 16.w, 8.h),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: () {
            if (!isRead) {
              context
                  .read<NotificationsBloc>()
                  .add(MarkNotificationRead(notification.id));
            }
          },
          borderRadius: BorderRadius.circular(12.r),
          child: Ink(
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(12.r),
              border: Border.all(
                color: isRead
                    ? AppColors.surfaceVariant
                    : AppColors.primary.withValues(alpha: 0.28),
                width: isRead ? 1 : 1.5,
              ),
              boxShadow: [
                BoxShadow(
                  color: isRead
                      ? AppColors.foreground.withValues(alpha: 0.04)
                      : AppColors.primary.withValues(alpha: 0.08),
                  blurRadius: 10,
                  offset: const Offset(0, 3),
                ),
              ],
            ),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(12.r),
              child: IntrinsicHeight(
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Left accent bar for unread
                    if (!isRead)
                      Container(
                        width: 3.w,
                        decoration: const BoxDecoration(
                          gradient: LinearGradient(
                            begin: Alignment.topCenter,
                            end: Alignment.bottomCenter,
                            colors: [
                              AppColors.primaryDark,
                              AppColors.primaryLight,
                            ],
                          ),
                        ),
                      ),
                    Expanded(
                      child: Padding(
                        padding: EdgeInsets.fromLTRB(
                          isRead ? 14.w : 12.w,
                          14.h,
                          14.w,
                          14.h,
                        ),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            // Icon box
                            Container(
                              width: 38.r,
                              height: 38.r,
                              decoration: BoxDecoration(
                                color: isRead
                                    ? AppColors.surface
                                    : AppColors.primary
                                        .withValues(alpha: 0.1),
                                borderRadius: BorderRadius.circular(10.r),
                                border: isRead
                                    ? Border.all(
                                        color: AppColors.surfaceVariant)
                                    : null,
                              ),
                              child: Icon(
                                iconData,
                                size: 18.r,
                                color: isRead
                                    ? AppColors.foregroundMuted
                                    : AppColors.primary,
                              ),
                            ),
                            SizedBox(width: 12.w),
                            // Content
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.center,
                                    children: [
                                      Expanded(
                                        child: Text(
                                          notification.title,
                                          style: GoogleFonts.barlowCondensed(
                                            fontSize: 15.sp,
                                            fontWeight: isRead
                                                ? FontWeight.w500
                                                : FontWeight.w700,
                                            letterSpacing: 0.3,
                                            color: AppColors.foreground,
                                          ),
                                        ),
                                      ),
                                      SizedBox(width: 8.w),
                                      Text(
                                        _relativeTime(notification.createdAt),
                                        style: GoogleFonts.barlow(
                                          fontSize: 10.sp,
                                          color: AppColors.foregroundMuted
                                              .withValues(alpha: 0.7),
                                        ),
                                      ),
                                    ],
                                  ),
                                  SizedBox(height: 4.h),
                                  Text(
                                    notification.body,
                                    style: GoogleFonts.barlow(
                                      fontSize: 13.sp,
                                      color: isRead
                                          ? AppColors.foregroundMuted
                                          : AppColors.foreground
                                              .withValues(alpha: 0.75),
                                      height: 1.4,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
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
