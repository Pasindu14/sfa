import 'package:uswatte/features/products/domain/entities/product.dart';

abstract interface class ProductsRepository {
  /// Returns the locally cached product list. Empty if never synced.
  Future<List<Product>> getProducts();

  /// Fetches all active products from the API, persists them locally,
  /// and returns the updated list together with the server's [cachedAt] timestamp.
  Future<(List<Product>, DateTime)> syncProducts();

  /// The timestamp of the last successful sync, or null if never synced.
  Future<DateTime?> getLastSyncedAt();
}
