import '../../domain/entities/route_unlock_detail.dart';
import '../../domain/entities/route_unlock_request.dart';
import '../../domain/repositories/route_unlock_repository.dart';
import '../datasources/route_unlock_remote_datasource.dart';

/// Remote-only: request status is always read live, and the unlock itself
/// reaches the device through the outlet sync's geofence policy — so there is
/// nothing to cache locally (and nothing for DeviceUserGuard to wipe).
class RouteUnlockRepositoryImpl implements RouteUnlockRepository {
  final RouteUnlockRemoteDatasource _remote;
  const RouteUnlockRepositoryImpl(this._remote);

  @override
  Future<RouteUnlockRequest?> getMyToday() => _remote.getMyToday();

  @override
  Future<RouteUnlockRequest> request({
    required String reason,
    double? latitude,
    double? longitude,
    double? gpsAccuracyMeters,
  }) =>
      _remote.request(
        reason: reason,
        latitude: latitude,
        longitude: longitude,
        gpsAccuracyMeters: gpsAccuracyMeters,
      );

  @override
  Future<RouteUnlockRequest> cancel(int id, int rowVersion) =>
      _remote.cancel(id, rowVersion);

  @override
  Future<List<RouteUnlockRequest>> getRequests({
    String? status,
    DateTime? from,
    DateTime? to,
    String? search,
    int page = 1,
    int pageSize = 50,
  }) =>
      _remote.getRequests(
        status: status,
        from: from,
        to: to,
        search: search,
        page: page,
        pageSize: pageSize,
      );

  @override
  Future<int> getPendingCount() => _remote.getPendingCount();

  @override
  Future<RouteUnlockDetail> getDetail(int id) => _remote.getDetail(id);

  @override
  Future<RouteUnlockRequest> approve(int id, int rowVersion, {String? note}) =>
      _remote.approve(id, rowVersion, note: note);

  @override
  Future<RouteUnlockRequest> reject(int id, int rowVersion, String reason) =>
      _remote.reject(id, rowVersion, reason);

  @override
  Future<RouteUnlockRequest> revoke(int id, int rowVersion, String reason) =>
      _remote.revoke(id, rowVersion, reason);
}
