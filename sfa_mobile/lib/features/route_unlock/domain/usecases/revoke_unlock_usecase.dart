import '../entities/route_unlock_request.dart';
import '../repositories/route_unlock_repository.dart';

class RevokeUnlockUseCase {
  final RouteUnlockRepository _repo;
  const RevokeUnlockUseCase(this._repo);

  Future<RouteUnlockRequest> call(int id, int rowVersion, String reason) =>
      _repo.revoke(id, rowVersion, reason);
}
