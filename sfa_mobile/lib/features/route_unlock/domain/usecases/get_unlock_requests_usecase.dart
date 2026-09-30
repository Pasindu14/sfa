import '../entities/route_unlock_request.dart';
import '../repositories/route_unlock_repository.dart';

class GetUnlockRequestsUseCase {
  final RouteUnlockRepository _repo;
  const GetUnlockRequestsUseCase(this._repo);

  Future<List<RouteUnlockRequest>> call({
    String? status,
    DateTime? from,
    DateTime? to,
    String? search,
    int page = 1,
    int pageSize = 50,
  }) =>
      _repo.getRequests(
        status: status,
        from: from,
        to: to,
        search: search,
        page: page,
        pageSize: pageSize,
      );
}
