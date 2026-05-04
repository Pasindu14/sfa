import 'package:uswatte/features/todays_route_map/domain/entities/route_map_outlet.dart';

abstract class SupervisorRouteMapRepository {
  Future<List<RouteMapOutlet>> getRepTodayRouteMap(int userId, DateTime date);
}
