import '../entities/route_unlock_detail.dart';
import '../repositories/route_unlock_repository.dart';

class GetUnlockRequestDetailUseCase {
  final RouteUnlockRepository _repo;
  const GetUnlockRequestDetailUseCase(this._repo);

  Future<RouteUnlockDetail> call(int id) => _repo.getDetail(id);
}
