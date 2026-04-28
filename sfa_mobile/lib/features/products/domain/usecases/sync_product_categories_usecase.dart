import 'package:uswatte/features/products/domain/entities/product_category.dart';
import 'package:uswatte/features/products/domain/repositories/product_categories_repository.dart';

class SyncProductCategoriesUseCase {
  final ProductCategoriesRepository _repository;
  const SyncProductCategoriesUseCase(this._repository);

  Future<(List<ProductCategory>, DateTime)> call() => _repository.syncCategories();
}
