import 'package:uswatte/features/outlets/data/models/outlet_model.dart';
import 'package:uswatte/features/supervisor_route_map/data/datasources/supervisor_route_map_remote_datasource.dart';
import 'package:uswatte/features/supervisor_route_map/domain/repositories/supervisor_route_map_repository.dart';
import 'package:uswatte/features/todays_route_map/domain/entities/route_map_outlet.dart';
import 'package:uswatte/features/todays_route_map/domain/enums/route_outlet_status.dart';

class SupervisorRouteMapRepositoryImpl implements SupervisorRouteMapRepository {
  final SupervisorRouteMapRemoteDatasource _datasource;

  const SupervisorRouteMapRepositoryImpl(this._datasource);

  @override
  Future<List<RouteMapOutlet>> getRepTodayRouteMap(
      int userId, DateTime date) async {
    final dateStr = _fmt(date);

    final routeId = await _datasource.getRepTodayRouteId(userId, dateStr);
    if (routeId == null) return [];

    final results = await Future.wait([
      _datasource.getOutletsByRoute(routeId),
      _datasource.getRepBilledOutletIds(userId, dateStr),
      _datasource.getRepNotBilledOutletIds(userId, dateStr),
    ]);

    final outlets = results[0] as List;
    final billedIds = results[1] as Set<int>;
    final notBilledIds = results[2] as Set<int>;

    return outlets.map((m) {
      final outlet = (m as OutletModel).toEntity();
      final RouteOutletStatus status;
      if (billedIds.contains(outlet.id)) {
        status = RouteOutletStatus.billed;
      } else if (notBilledIds.contains(outlet.id)) {
        status = RouteOutletStatus.notBilled;
      } else {
        status = RouteOutletStatus.pending;
      }
      return RouteMapOutlet(outlet: outlet, status: status);
    }).toList();
  }

  String _fmt(DateTime dt) =>
      '${dt.year.toString().padLeft(4, '0')}-'
      '${dt.month.toString().padLeft(2, '0')}-'
      '${dt.day.toString().padLeft(2, '0')}';
}
