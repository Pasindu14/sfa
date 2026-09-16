import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/outlets/domain/entities/proximity_policy.dart';
import 'package:uswatte/features/outlets/domain/repositories/outlets_repository.dart';

class SyncOutletsUseCase {
  final OutletsRepository _repository;
  const SyncOutletsUseCase(this._repository);

  Future<({List<Outlet> outlets, ProximityPolicy policy})> call(
          int routeId, String routeName) =>
      _repository.syncOutlets(routeId, routeName);
}
