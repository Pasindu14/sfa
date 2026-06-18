import 'package:uswatte/features/outlets/domain/entities/outlet.dart';

abstract interface class OutletsRepository {
  Future<List<Outlet>> getOutlets();
  Future<({List<Outlet> outlets, double geofenceRadiusMeters})> syncOutlets(
      int routeId, String routeName);
  Future<DateTime?> getLastSyncedAt();
  Future<int?> getCurrentRouteId();
  Future<double?> getGeofenceRadiusMeters();
}
