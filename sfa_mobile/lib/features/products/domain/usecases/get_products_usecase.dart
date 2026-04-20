import 'package:uswatte/features/products/domain/entities/product.dart';
import 'package:uswatte/features/products/domain/repositories/products_repository.dart';

class GetProductsUseCase {
  final ProductsRepository _repository;
  const GetProductsUseCase(this._repository);

  Future<List<Product>> call() => _repository.getProducts();
}
