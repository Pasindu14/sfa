import '../entities/route_unlock_request.dart';
import '../repositories/route_unlock_repository.dart';

class RequestRouteUnlockUseCase {
  final RouteUnlockRepository _repo;
  const RequestRouteUnlockUseCase(this._repo);

  Future<RouteUnlockRequest> call({
    required String reason,
    double? latitude,
    double? longitude,
    double? gpsAccuracyMeters,
  }) =>
      _repo.request(
        reason: reason,
        latitude: latitude,
        longitude: longitude,
        gpsAccuracyMeters: gpsAccuracyMeters,
      );
}
