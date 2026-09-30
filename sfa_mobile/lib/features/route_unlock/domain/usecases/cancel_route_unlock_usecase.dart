import '../entities/route_unlock_request.dart';
import '../repositories/route_unlock_repository.dart';

class CancelRouteUnlockUseCase {
  final RouteUnlockRepository _repo;
  const CancelRouteUnlockUseCase(this._repo);

  Future<RouteUnlockRequest> call(int id, int rowVersion) =>
      _repo.cancel(id, rowVersion);
}
