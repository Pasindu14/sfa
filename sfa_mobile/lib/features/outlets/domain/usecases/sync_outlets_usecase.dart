import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/outlets/domain/repositories/outlets_repository.dart';

class SyncOutletsUseCase {
  final OutletsRepository _repository;
  const SyncOutletsUseCase(this._repository);

  Future<({List<Outlet> outlets, double geofenceRadiusMeters})> call(
          int routeId, String routeName) =>
      _repository.syncOutlets(routeId, routeName);
}
