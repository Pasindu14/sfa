import 'package:uswatte/features/supervisor_route_map/domain/entities/rep_last_location.dart';
import 'package:uswatte/features/todays_route_map/domain/entities/route_map_outlet.dart';

abstract class SupervisorRouteMapRepository {
  Future<List<RouteMapOutlet>> getRepTodayRouteMap(int userId, DateTime date);

  /// The rep's latest location ping, or null if none was ever received.
  Future<RepLastLocation?> getRepLastLocation(int userId);
}
