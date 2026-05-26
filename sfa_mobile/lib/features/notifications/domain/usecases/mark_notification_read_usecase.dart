import '../repositories/notifications_repository.dart';

class MarkNotificationReadUseCase {
  final NotificationsRepository _repo;
  const MarkNotificationReadUseCase(this._repo);

  Future<void> call(int id) => _repo.markRead(id);
}
