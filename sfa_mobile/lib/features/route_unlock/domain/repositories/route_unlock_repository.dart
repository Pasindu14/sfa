import '../entities/route_unlock_detail.dart';
import '../entities/route_unlock_request.dart';

abstract class RouteUnlockRepository {
  // ── Rep ────────────────────────────────────────────────────────────────────
  /// The rep's latest request for today, or null when there is none.
  Future<RouteUnlockRequest?> getMyToday();
  Future<RouteUnlockRequest> request({
    required String reason,
    double? latitude,
    double? longitude,
    double? gpsAccuracyMeters,
  });
  Future<RouteUnlockRequest> cancel(int id, int rowVersion);

  // ── Supervisor ─────────────────────────────────────────────────────────────
  /// [status] takes an effectiveStatus value; [from]/[to] are business dates.
  Future<List<RouteUnlockRequest>> getRequests({
    String? status,
    DateTime? from,
    DateTime? to,
    String? search,
    int page = 1,
    int pageSize = 50,
  });
  Future<int> getPendingCount();
  Future<RouteUnlockDetail> getDetail(int id);
  Future<RouteUnlockRequest> approve(int id, int rowVersion, {String? note});
  Future<RouteUnlockRequest> reject(int id, int rowVersion, String reason);
  Future<RouteUnlockRequest> revoke(int id, int rowVersion, String reason);
}
