import '../repositories/route_unlock_repository.dart';

class GetPendingUnlockCountUseCase {
  final RouteUnlockRepository _repo;
  const GetPendingUnlockCountUseCase(this._repo);

  Future<int> call() => _repo.getPendingCount();
}
