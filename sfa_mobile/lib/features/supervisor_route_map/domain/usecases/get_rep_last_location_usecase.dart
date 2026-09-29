import 'package:uswatte/features/supervisor_route_map/domain/entities/rep_last_location.dart';
import 'package:uswatte/features/supervisor_route_map/domain/repositories/supervisor_route_map_repository.dart';

class GetRepLastLocationUseCase {
  final SupervisorRouteMapRepository _repository;
  const GetRepLastLocationUseCase(this._repository);

  Future<RepLastLocation?> call(int userId) =>
      _repository.getRepLastLocation(userId);
}
