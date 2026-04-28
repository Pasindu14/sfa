import 'package:uswatte/features/products/domain/entities/product_category.dart';
import 'package:uswatte/features/products/domain/repositories/product_categories_repository.dart';

class GetProductCategoriesUseCase {
  final ProductCategoriesRepository _repository;
  const GetProductCategoriesUseCase(this._repository);

  Future<List<ProductCategory>> call() => _repository.getCategories();
}
