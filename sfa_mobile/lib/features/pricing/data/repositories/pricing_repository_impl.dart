import 'package:uswatte/core/network/conditional_get.dart';
import 'package:uswatte/core/sync/etag_store.dart';
import 'package:uswatte/features/pricing/data/datasources/pricing_local_datasource.dart';
import 'package:uswatte/features/pricing/data/datasources/pricing_remote_datasource.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';
import 'package:uswatte/features/pricing/domain/repositories/pricing_repository.dart';

class PricingRepositoryImpl implements PricingRepository {
  final PricingRemoteDatasource _remote;
  final PricingLocalDatasource _local;
  final EtagStore _etags;
  final DateTime Function() _now;

  PricingRepositoryImpl(
    this._remote,
    this._local,
    this._etags, {
    DateTime Function()? clock,
  }) : _now = clock ?? DateTime.now;

  @override
  Future<List<PricingStructure>> getPricingStructures() async {
    final models = await _local.getAllStructures();
    return models.map((m) => m.toEntity()).toList();
  }

  @override
  Future<(List<PricingStructure>, DateTime)> syncPricingStructures({
    bool force = false,
  }) {
    // Serialized: two overlapping syncs could otherwise interleave their
    // replace and ETag writes and pair one response's rows with the other's
    // ETag. The second caller usually gets a cheap 304 instead.
    return _serialized(() => _sync(force: force));
  }

  Future<(List<PricingStructure>, DateTime)> _sync({
    required bool force,
  }) async {
    final ifNoneMatch = force ? null : await _conditionalEtag();
    final result =
        await _remote.getPricingStructures(ifNoneMatch: ifNoneMatch);

    switch (result) {
      case NotModified():
        // The server confirmed the local tables are current: keep them, and
        // stamp the check so "last synced" reflects it.
        final checkedAt = _now().toUtc();
        await _local.saveLastSyncedAt(checkedAt);
        return (await getPricingStructures(), checkedAt);

      case Fetched(:final data, :final etag):
        // Forget the old ETag before touching the tables, and only record the
        // new one once the replace has committed — so a failure anywhere in
        // between can never pair an ETag with rows it doesn't describe.
        await _tryEtag(() => _etags.clear(EtagStore.pricingStructures));
        await _local.replaceAll(data.pricingStructures);
        if (etag != null) {
          await _tryEtag(() => _etags.write(EtagStore.pricingStructures, etag));
        }
        await _local.saveLastSyncedAt(data.cachedAt);
        return (
          data.pricingStructures.map((m) => m.toEntity()).toList(),
          data.cachedAt,
        );
    }
  }

  @override
  Future<DateTime?> getLastSyncedAt() => _local.getLastSyncedAt();

  Future<void> _tail = Future.value();

  Future<T> _serialized<T>(Future<T> Function() op) {
    final result = _tail.then((_) => op());
    _tail = result.then<void>((_) {}, onError: (Object _) {});
    return result;
  }

  /// The stored ETag, but only when there is local data for a 304 to keep.
  Future<String?> _conditionalEtag() async {
    try {
      final etag = await _etags.read(EtagStore.pricingStructures);
      if (etag == null) return null;
      return await _local.hasAny() ? etag : null;
    } catch (_) {
      return null; // Can't tell — fall back to a full download.
    }
  }

  /// ETag bookkeeping is an optimisation; failing it must not fail a sync
  /// whose data already landed.
  Future<void> _tryEtag(Future<void> Function() op) async {
    try {
      await op();
    } catch (_) {}
  }
}
