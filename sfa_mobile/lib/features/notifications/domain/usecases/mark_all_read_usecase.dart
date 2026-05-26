import '../repositories/notifications_repository.dart';

class MarkAllReadUseCase {
  final NotificationsRepository _repo;
  const MarkAllReadUseCase(this._repo);

  Future<void> call() => _repo.markAllRead();
}
