part of 'notifications_bloc.dart';

sealed class NotificationsEvent {
  const NotificationsEvent();
}

final class LoadNotifications extends NotificationsEvent {
  const LoadNotifications();
}

final class LoadMoreNotifications extends NotificationsEvent {
  const LoadMoreNotifications();
}

final class MarkNotificationRead extends NotificationsEvent {
  final int id;
  const MarkNotificationRead(this.id);
}

final class MarkAllNotificationsRead extends NotificationsEvent {
  const MarkAllNotificationsRead();
}
