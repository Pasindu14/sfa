import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/outlets/domain/entities/proximity_policy.dart';

abstract interface class OutletsRepository {
  Future<List<Outlet>> getOutlets();
  Future<({List<Outlet> outlets, ProximityPolicy policy})> syncOutlets(
      int routeId, String routeName);
  Future<DateTime?> getLastSyncedAt();
  Future<int?> getCurrentRouteId();

  /// The last policy pushed down by the server, or null before the first sync.
  Future<ProximityPolicy?> getProximityPolicy();
  Future<void> clearDailyOutlets();
}
