import '../entities/notification_entity.dart';
import '../repositories/notifications_repository.dart';

class GetNotificationsUseCase {
  final NotificationsRepository _repo;
  const GetNotificationsUseCase(this._repo);

  Future<(List<NotificationEntity>, int)> call({
    required int page,
    int pageSize = 20,
  }) =>
      _repo.getNotifications(page: page, pageSize: pageSize);
}
