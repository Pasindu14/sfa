import 'package:uswatte/features/products/data/datasources/products_local_datasource.dart';
import 'package:uswatte/features/products/data/datasources/products_remote_datasource.dart';
import 'package:uswatte/features/products/domain/entities/product.dart';
import 'package:uswatte/features/products/domain/repositories/products_repository.dart';

class ProductsRepositoryImpl implements ProductsRepository {
  final ProductsRemoteDatasource _remote;
  final ProductsLocalDatasource _local;

  const ProductsRepositoryImpl(this._remote, this._local);

  @override
  Future<List<Product>> getProducts() async {
    final models = await _local.getAllProducts();
    return models.map((m) => m.toEntity()).toList();
  }

  @override
  Future<(List<Product>, DateTime)> syncProducts() async {
    final response = await _remote.getProducts();
    await _local.replaceAll(response.products);
    await _local.saveLastSyncedAt(response.cachedAt);
    return (
      response.products.map((m) => m.toEntity()).toList(),
      response.cachedAt,
    );
  }

  @override
  Future<DateTime?> getLastSyncedAt() => _local.getLastSyncedAt();
}
