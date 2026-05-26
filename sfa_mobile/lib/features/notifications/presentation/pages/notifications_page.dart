import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import '../bloc/notifications_bloc.dart';

class NotificationsPage extends StatelessWidget {
  const NotificationsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        backgroundColor: AppColors.background,
        elevation: 0,
        title: Text(
          'Notifications',
          style: GoogleFonts.barlowCondensed(
            fontSize: 20.sp,
            fontWeight: FontWeight.w700,
            color: AppColors.foreground,
          ),
        ),
        actions: [
          BlocBuilder<NotificationsBloc, NotificationsState>(
            builder: (context, state) {
              if (state is! NotificationsLoaded || state.unreadCount == 0) {
                return const SizedBox.shrink();
              }
              return TextButton(
                onPressed: () =>
                    context.read<NotificationsBloc>().add(const MarkAllNotificationsRead()),
                child: Text(
                  'Mark all read',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 13.sp,
                    fontWeight: FontWeight.w600,
                    color: AppColors.primary,
                  ),
                ),
              );
            },
          ),
        ],
      ),
      body: BlocBuilder<NotificationsBloc, NotificationsState>(
        builder: (context, state) {
          if (state is NotificationsLoading) {
            return const Center(
              child: CircularProgressIndicator(color: AppColors.primary),
            );
          }

          if (state is NotificationsError) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(Icons.error_outline, size: 40.r, color: AppColors.error),
                  SizedBox(height: 12.h),
                  Text(state.message,
                      style: TextStyle(color: AppColors.foregroundMuted, fontSize: 14.sp)),
                  SizedBox(height: 16.h),
                  ElevatedButton(
                    onPressed: () => context
                        .read<NotificationsBloc>()
                        .add(const LoadNotifications()),
                    child: const Text('Retry'),
                  ),
                ],
              ),
            );
          }

          final items = switch (state) {
            NotificationsLoaded(items: final i) => i,
            NotificationsLoadingMore(items: final i) => i,
            _ => <dynamic>[],
          };

          if (items.isEmpty) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(Icons.notifications_none_outlined,
                      size: 48.r, color: AppColors.foregroundMuted),
                  SizedBox(height: 12.h),
                  Text(
                    'No notifications yet',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 16.sp,
                      color: AppColors.foregroundMuted,
                    ),
                  ),
                ],
              ),
            );
          }

          return RefreshIndicator(
            color: AppColors.primary,
            onRefresh: () async {
              context.read<NotificationsBloc>().add(const LoadNotifications());
              await context.read<NotificationsBloc>().stream
                  .firstWhere((s) => s is NotificationsLoaded || s is NotificationsError);
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
              child: ListView.builder(
                padding: EdgeInsets.symmetric(vertical: 8.h),
                itemCount: items.length + (state is NotificationsLoadingMore ? 1 : 0),
                itemBuilder: (context, index) {
                  if (index == items.length) {
                    return Padding(
                      padding: EdgeInsets.all(16.r),
                      child: const Center(
                        child: CircularProgressIndicator(color: AppColors.primary),
                      ),
                    );
                  }
                  final notification = items[index];
                  return _NotificationTile(notification: notification);
                },
              ),
            ),
          );
        },
      ),
    );
  }
}

class _NotificationTile extends StatelessWidget {
  final dynamic notification;
  const _NotificationTile({required this.notification});

  String _relativeTime(DateTime createdAt) {
    final diff = DateTime.now().difference(createdAt);
    if (diff.inMinutes < 1) return 'Just now';
    if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
    if (diff.inHours < 24) return '${diff.inHours}h ago';
    if (diff.inDays < 7) return '${diff.inDays}d ago';
    return '${createdAt.day}/${createdAt.month}/${createdAt.year}';
  }

  @override
  Widget build(BuildContext context) {
    final isRead = notification.isRead as bool;
    return InkWell(
      onTap: () {
        if (!isRead) {
          context
              .read<NotificationsBloc>()
              .add(MarkNotificationRead(notification.id as int));
        }
      },
      child: Container(
        color: isRead ? AppColors.background : AppColors.surface,
        padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 14.h),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (!isRead)
              Container(
                width: 8.r,
                height: 8.r,
                margin: EdgeInsets.only(top: 5.h, right: 10.w),
                decoration: const BoxDecoration(
                  color: AppColors.primary,
                  shape: BoxShape.circle,
                ),
              )
            else
              SizedBox(width: 18.w),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    notification.title as String,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 15.sp,
                      fontWeight:
                          isRead ? FontWeight.w500 : FontWeight.w700,
                      color: AppColors.foreground,
                    ),
                  ),
                  SizedBox(height: 3.h),
                  Text(
                    notification.body as String,
                    style: TextStyle(
                      fontSize: 13.sp,
                      color: AppColors.foregroundMuted,
                      height: 1.4,
                    ),
                  ),
                  SizedBox(height: 6.h),
                  Text(
                    _relativeTime(notification.createdAt as DateTime),
                    style: TextStyle(
                      fontSize: 11.sp,
                      color: AppColors.foregroundMuted,
                    ),
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
