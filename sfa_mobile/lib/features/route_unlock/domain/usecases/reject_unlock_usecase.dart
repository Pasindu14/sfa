import '../entities/route_unlock_request.dart';
import '../repositories/route_unlock_repository.dart';

class RejectUnlockUseCase {
  final RouteUnlockRepository _repo;
  const RejectUnlockUseCase(this._repo);

  Future<RouteUnlockRequest> call(int id, int rowVersion, String reason) =>
      _repo.reject(id, rowVersion, reason);
}
