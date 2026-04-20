import 'package:uswatte/features/products/domain/entities/product.dart';
import 'package:uswatte/features/products/domain/repositories/products_repository.dart';

class SyncProductsUseCase {
  final ProductsRepository _repository;
  const SyncProductsUseCase(this._repository);

  Future<(List<Product>, DateTime)> call() => _repository.syncProducts();
}
