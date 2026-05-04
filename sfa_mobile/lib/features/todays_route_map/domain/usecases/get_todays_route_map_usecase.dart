import 'package:uswatte/features/todays_route_map/domain/entities/route_map_outlet.dart';
import 'package:uswatte/features/todays_route_map/domain/repositories/todays_route_map_repository.dart';

class GetTodaysRouteMapUseCase {
  final TodaysRouteMapRepository _repository;
  const GetTodaysRouteMapUseCase(this._repository);

  Future<({List<RouteMapOutlet> outlets, int? lastBilledOutletId})> call() =>
      _repository.getTodaysRouteOutlets();
}
