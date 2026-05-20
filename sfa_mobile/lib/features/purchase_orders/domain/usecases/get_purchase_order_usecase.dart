import '../entities/purchase_order_detail.dart';
import '../repositories/purchase_orders_repository.dart';

class GetPurchaseOrderUseCase {
  final PurchaseOrdersRepository _repo;
  const GetPurchaseOrderUseCase(this._repo);

  Future<PurchaseOrderDetail> call(int id) => _repo.getOrderById(id);
}
