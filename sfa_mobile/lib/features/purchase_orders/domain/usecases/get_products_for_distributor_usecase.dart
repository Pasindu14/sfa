import '../entities/product_with_price.dart';
import '../repositories/purchase_orders_repository.dart';

class GetProductsForDistributorUseCase {
  final PurchaseOrdersRepository _repo;
  const GetProductsForDistributorUseCase(this._repo);

  Future<List<ProductWithPrice>> call(int distributorId) =>
      _repo.getProductsForDistributor(distributorId);
}
