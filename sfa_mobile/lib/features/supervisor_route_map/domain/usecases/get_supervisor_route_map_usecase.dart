import 'package:uswatte/features/supervisor_route_map/domain/repositories/supervisor_route_map_repository.dart';
import 'package:uswatte/features/todays_route_map/domain/entities/route_map_outlet.dart';

class GetSupervisorRouteMapUseCase {
  final SupervisorRouteMapRepository _repository;
  const GetSupervisorRouteMapUseCase(this._repository);

  Future<List<RouteMapOutlet>> call(int userId, DateTime date) =>
      _repository.getRepTodayRouteMap(userId, date);
}
