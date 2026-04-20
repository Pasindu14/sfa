import 'package:uswatte/features/pricing/data/datasources/pricing_local_datasource.dart';
import 'package:uswatte/features/pricing/data/datasources/pricing_remote_datasource.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';
import 'package:uswatte/features/pricing/domain/repositories/pricing_repository.dart';

class PricingRepositoryImpl implements PricingRepository {
  final PricingRemoteDatasource _remote;
  final PricingLocalDatasource _local;

  const PricingRepositoryImpl(this._remote, this._local);

  @override
  Future<List<PricingStructure>> getPricingStructures() async {
    final models = await _local.getAllStructures();
    return models.map((m) => m.toEntity()).toList();
  }

  @override
  Future<(List<PricingStructure>, DateTime)> syncPricingStructures() async {
    final models = await _remote.getPricingStructures();
    await _local.replaceAll(models);
    final syncedAt = DateTime.now();
    await _local.saveLastSyncedAt(syncedAt);
    return (models.map((m) => m.toEntity()).toList(), syncedAt);
  }

  @override
  Future<DateTime?> getLastSyncedAt() => _local.getLastSyncedAt();
}
