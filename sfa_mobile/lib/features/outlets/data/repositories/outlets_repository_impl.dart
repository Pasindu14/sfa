import 'package:uswatte/features/outlets/data/datasources/outlets_local_datasource.dart';
import 'package:uswatte/features/outlets/data/datasources/outlets_remote_datasource.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/outlets/domain/repositories/outlets_repository.dart';

class OutletsRepositoryImpl implements OutletsRepository {
  final OutletsRemoteDatasource _remote;
  final OutletsLocalDatasource _local;

  const OutletsRepositoryImpl(this._remote, this._local);

  @override
  Future<List<Outlet>> getOutlets() async {
    final models = await _local.getAllOutlets();
    return models.map((m) => m.toEntity()).toList();
  }

  @override
  Future<List<Outlet>> syncOutlets(int routeId, String routeName) async {
    await _local.saveCurrentRoute(routeId, routeName);
    final models = await _remote.getOutletsByRoute(routeId);
    await _local.replaceAll(models);
    await _local.saveLastSyncedAt(DateTime.now());
    return models.map((m) => m.toEntity()).toList();
  }

  @override
  Future<DateTime?> getLastSyncedAt() => _local.getLastSyncedAt();

  @override
  Future<int?> getCurrentRouteId() => _local.getCurrentRouteId();
}
