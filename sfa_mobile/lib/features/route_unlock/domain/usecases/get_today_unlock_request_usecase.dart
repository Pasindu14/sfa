import '../entities/route_unlock_request.dart';
import '../repositories/route_unlock_repository.dart';

class GetTodayUnlockRequestUseCase {
  final RouteUnlockRepository _repo;
  const GetTodayUnlockRequestUseCase(this._repo);

  Future<RouteUnlockRequest?> call() => _repo.getMyToday();
}
