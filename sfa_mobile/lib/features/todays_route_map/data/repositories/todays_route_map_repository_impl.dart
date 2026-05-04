import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/not_billings/data/datasources/not_billings_local_datasource.dart';
import 'package:uswatte/features/outlets/data/datasources/outlets_local_datasource.dart';
import 'package:uswatte/features/todays_route_map/domain/entities/route_map_outlet.dart';
import 'package:uswatte/features/todays_route_map/domain/enums/route_outlet_status.dart';
import 'package:uswatte/features/todays_route_map/domain/repositories/todays_route_map_repository.dart';

class TodaysRouteMapRepositoryImpl implements TodaysRouteMapRepository {
  final OutletsLocalDatasource _outletsDatasource;
  final BillsLocalDatasource _billsDatasource;
  final NotBillingsLocalDatasource _notBillingsDatasource;

  const TodaysRouteMapRepositoryImpl({
    required OutletsLocalDatasource outletsDatasource,
    required BillsLocalDatasource billsDatasource,
    required NotBillingsLocalDatasource notBillingsDatasource,
  })  : _outletsDatasource = outletsDatasource,
        _billsDatasource = billsDatasource,
        _notBillingsDatasource = notBillingsDatasource;

  @override
  Future<({List<RouteMapOutlet> outlets, int? lastBilledOutletId})>
      getTodaysRouteOutlets() async {
    final results = await Future.wait([
      _outletsDatasource.getAllOutlets(),
      _billsDatasource.getTodaysBilledOutletIds(),
      _notBillingsDatasource.getTodaysNotBilledOutletIds(),
      _billsDatasource.getTodaysMostRecentBilledOutletId(),
    ]);

    final outlets = results[0] as List;
    final billedIds = results[1] as Set<int>;
    final notBilledIds = results[2] as Set<int>;
    final lastBilledOutletId = results[3] as int?;

    final routeOutlets = outlets.map((model) {
      final outlet = model.toEntity();
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

    return (outlets: routeOutlets, lastBilledOutletId: lastBilledOutletId);
  }
}
