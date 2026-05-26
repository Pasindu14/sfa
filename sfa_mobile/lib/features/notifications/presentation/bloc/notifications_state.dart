part of 'notifications_bloc.dart';

sealed class NotificationsState {
  const NotificationsState();
}

final class NotificationsInitial extends NotificationsState {
  const NotificationsInitial();
}

final class NotificationsLoading extends NotificationsState {
  const NotificationsLoading();
}

final class NotificationsLoaded extends NotificationsState {
  final List<NotificationEntity> items;
  final int page;
  final bool hasMore;
  final int unreadCount;

  const NotificationsLoaded({
    required this.items,
    required this.page,
    required this.hasMore,
    required this.unreadCount,
  });

  NotificationsLoaded copyWith({
    List<NotificationEntity>? items,
    int? page,
    bool? hasMore,
    int? unreadCount,
  }) =>
      NotificationsLoaded(
        items: items ?? this.items,
        page: page ?? this.page,
        hasMore: hasMore ?? this.hasMore,
        unreadCount: unreadCount ?? this.unreadCount,
      );
}

final class NotificationsLoadingMore extends NotificationsState {
  final List<NotificationEntity> items;
  final int page;

  const NotificationsLoadingMore({required this.items, required this.page});
}

final class NotificationsError extends NotificationsState {
  final String message;
  const NotificationsError(this.message);
}
