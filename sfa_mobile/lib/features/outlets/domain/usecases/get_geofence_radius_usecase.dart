import 'package:uswatte/features/outlets/domain/repositories/outlets_repository.dart';

class GetGeofenceRadiusUseCase {
  final OutletsRepository _repository;
  const GetGeofenceRadiusUseCase(this._repository);

  Future<double?> call() => _repository.getGeofenceRadiusMeters();
}
