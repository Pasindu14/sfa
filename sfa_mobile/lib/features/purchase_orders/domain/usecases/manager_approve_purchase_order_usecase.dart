import '../repositories/purchase_orders_repository.dart';

class ManagerApprovePurchaseOrderUseCase {
  final PurchaseOrdersRepository _repo;
  const ManagerApprovePurchaseOrderUseCase(this._repo);

  Future<void> call(int id) => _repo.managerApprove(id);
}
