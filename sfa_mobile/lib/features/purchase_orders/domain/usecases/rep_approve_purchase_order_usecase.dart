import '../repositories/purchase_orders_repository.dart';

class RepApprovePurchaseOrderUseCase {
  final PurchaseOrdersRepository _repo;
  const RepApprovePurchaseOrderUseCase(this._repo);

  Future<void> call(int id) => _repo.repApprove(id);
}
