import 'package:uswatte/features/todays_route_map/domain/entities/route_map_outlet.dart';

abstract class TodaysRouteMapRepository {
  Future<({List<RouteMapOutlet> outlets, int? lastBilledOutletId})> getTodaysRouteOutlets();
}
