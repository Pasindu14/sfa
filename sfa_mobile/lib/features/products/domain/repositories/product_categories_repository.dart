import 'package:uswatte/features/products/domain/entities/product_category.dart';

abstract interface class ProductCategoriesRepository {
  /// Returns the locally cached category list. Empty if never synced.
  Future<List<ProductCategory>> getCategories();

  /// Fetches all active categories from the API, persists them locally,
  /// and returns the updated list together with the server's [cachedAt] timestamp.
  Future<(List<ProductCategory>, DateTime)> syncCategories();

  /// The timestamp of the last successful sync, or null if never synced.
  Future<DateTime?> getLastSyncedAt();
}
