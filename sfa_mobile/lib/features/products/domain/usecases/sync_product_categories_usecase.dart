import 'package:uswatte/features/products/domain/entities/product_category.dart';
import 'package:uswatte/features/products/domain/repositories/product_categories_repository.dart';

class SyncProductCategoriesUseCase {
  final ProductCategoriesRepository _repository;
  final DateTime Function() _now;

  const SyncProductCategoriesUseCase(
    this._repository, {
    DateTime Function() clock = DateTime.now,
  }) : _now = clock;

  Future<(List<ProductCategory>, DateTime)> call({bool force = false}) =>
      _repository.syncCategories(force: force);

  /// Syncs only when the last recorded sync is missing or older than
  /// [maxAge]. Returns whether a sync ran (and succeeded), so callers can skip
  /// re-reading data that could not have changed. Errors from the sync itself
  /// propagate exactly as from [call].
  ///
  /// A last-synced time in the future (clock skew — the stamp can be the
  /// server's cachedAt) is treated as stale rather than trusted.
  Future<bool> syncIfStale(Duration maxAge) async {
    DateTime? last;
    try {
      last = await _repository.getLastSyncedAt();
    } catch (_) {
      last = null; // Can't tell — sync, as before.
    }
    if (last != null) {
      final age = _now().toUtc().difference(last.toUtc());
      if (!age.isNegative && age < maxAge) return false;
    }
    await _repository.syncCategories();
    return true;
  }
}
