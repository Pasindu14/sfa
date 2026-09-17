import 'package:uswatte/core/network/conditional_get.dart';
import 'package:uswatte/core/sync/etag_store.dart';
import 'package:uswatte/features/products/data/datasources/product_categories_local_datasource.dart';
import 'package:uswatte/features/products/data/datasources/product_categories_remote_datasource.dart';
import 'package:uswatte/features/products/domain/entities/product_category.dart';
import 'package:uswatte/features/products/domain/repositories/product_categories_repository.dart';

class ProductCategoriesRepositoryImpl implements ProductCategoriesRepository {
  final ProductCategoriesRemoteDatasource _remote;
  final ProductCategoriesLocalDatasource _local;
  final EtagStore _etags;
  final DateTime Function() _now;

  ProductCategoriesRepositoryImpl(
    this._remote,
    this._local,
    this._etags, {
    DateTime Function()? clock,
  }) : _now = clock ?? DateTime.now;

  @override
  Future<List<ProductCategory>> getCategories() async {
    final models = await _local.getAll();
    return models.map((m) => m.toEntity()).toList();
  }

  @override
  Future<(List<ProductCategory>, DateTime)> syncCategories({
    bool force = false,
  }) async {
    // Serialized: two overlapping syncs could otherwise interleave their
    // replace and ETag writes and pair one response's rows with the other's
    // ETag. The second caller usually gets a cheap 304 instead.
    return _serialized(() => _sync(force: force));
  }

  Future<(List<ProductCategory>, DateTime)> _sync({required bool force}) async {
    final ifNoneMatch = force ? null : await _conditionalEtag();
    final result = await _remote.getProductCategories(ifNoneMatch: ifNoneMatch);

    switch (result) {
      case NotModified():
        // The server confirmed the local table is current: keep it, and stamp
        // the check so "last synced" reflects it.
        final checkedAt = _now().toUtc();
        await _local.saveLastSyncedAt(checkedAt);
        final models = await _local.getAll();
        return (models.map((m) => m.toEntity()).toList(), checkedAt);

      case Fetched(:final data, :final etag):
        // Forget the old ETag before touching the table, and only record the
        // new one once the replace has committed — so a failure anywhere in
        // between can never pair an ETag with rows it doesn't describe.
        await _tryEtag(() => _etags.clear(EtagStore.productCategories));
        await _local.replaceAll(data.categories);
        if (etag != null) {
          await _tryEtag(
              () => _etags.write(EtagStore.productCategories, etag));
        }
        await _local.saveLastSyncedAt(data.cachedAt);
        return (
          data.categories.map((m) => m.toEntity()).toList(),
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
      final etag = await _etags.read(EtagStore.productCategories);
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
