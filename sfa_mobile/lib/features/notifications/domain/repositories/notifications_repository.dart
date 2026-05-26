import '../entities/notification_entity.dart';

abstract class NotificationsRepository {
  Future<(List<NotificationEntity> items, int totalCount)> getNotifications({
    required int page,
    required int pageSize,
  });
  Future<int> getUnreadCount();
  Future<void> markRead(int id);
  Future<void> markAllRead();
}
