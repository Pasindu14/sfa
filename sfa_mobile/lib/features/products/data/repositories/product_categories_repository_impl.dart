import 'package:uswatte/features/products/data/datasources/product_categories_local_datasource.dart';
import 'package:uswatte/features/products/data/datasources/product_categories_remote_datasource.dart';
import 'package:uswatte/features/products/domain/entities/product_category.dart';
import 'package:uswatte/features/products/domain/repositories/product_categories_repository.dart';

class ProductCategoriesRepositoryImpl implements ProductCategoriesRepository {
  final ProductCategoriesRemoteDatasource _remote;
  final ProductCategoriesLocalDatasource _local;

  const ProductCategoriesRepositoryImpl(this._remote, this._local);

  @override
  Future<List<ProductCategory>> getCategories() async {
    final models = await _local.getAll();
    return models.map((m) => m.toEntity()).toList();
  }

  @override
  Future<(List<ProductCategory>, DateTime)> syncCategories() async {
    final response = await _remote.getProductCategories();
    await _local.replaceAll(response.categories);
    await _local.saveLastSyncedAt(response.cachedAt);
    return (
      response.categories.map((m) => m.toEntity()).toList(),
      response.cachedAt,
    );
  }

  @override
  Future<DateTime?> getLastSyncedAt() => _local.getLastSyncedAt();
}
