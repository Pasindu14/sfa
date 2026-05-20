import '../repositories/purchase_orders_repository.dart';

class RejectPurchaseOrderUseCase {
  final PurchaseOrdersRepository _repo;
  const RejectPurchaseOrderUseCase(this._repo);

  Future<void> call(int id, String reason) => _repo.reject(id, reason);
}
