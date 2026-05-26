import '../repositories/notifications_repository.dart';

class GetUnreadCountUseCase {
  final NotificationsRepository _repo;
  const GetUnreadCountUseCase(this._repo);

  Future<int> call() => _repo.getUnreadCount();
}
