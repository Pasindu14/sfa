import '../entities/route_unlock_request.dart';
import '../repositories/route_unlock_repository.dart';

class ApproveUnlockUseCase {
  final RouteUnlockRepository _repo;
  const ApproveUnlockUseCase(this._repo);

  Future<RouteUnlockRequest> call(int id, int rowVersion, {String? note}) =>
      _repo.approve(id, rowVersion, note: note);
}
