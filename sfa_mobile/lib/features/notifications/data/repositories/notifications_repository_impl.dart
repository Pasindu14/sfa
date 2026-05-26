import '../../domain/entities/notification_entity.dart';
import '../../domain/repositories/notifications_repository.dart';
import '../datasources/notifications_remote_datasource.dart';

class NotificationsRepositoryImpl implements NotificationsRepository {
  final NotificationsRemoteDatasource _remote;
  const NotificationsRepositoryImpl(this._remote);

  @override
  Future<(List<NotificationEntity>, int)> getNotifications({
    required int page,
    required int pageSize,
  }) =>
      _remote.getNotifications(page: page, pageSize: pageSize);

  @override
  Future<int> getUnreadCount() => _remote.getUnreadCount();

  @override
  Future<void> markRead(int id) => _remote.markRead(id);

  @override
  Future<void> markAllRead() => _remote.markAllRead();
}
