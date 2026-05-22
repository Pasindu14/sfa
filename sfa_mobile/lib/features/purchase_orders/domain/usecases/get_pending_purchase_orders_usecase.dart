import '../entities/purchase_order_summary.dart';
import '../repositories/purchase_orders_repository.dart';

class GetPendingPurchaseOrdersUseCase {
  final PurchaseOrdersRepository _repo;
  const GetPendingPurchaseOrdersUseCase(this._repo);

  Future<List<PurchaseOrderSummary>> call({
    String status = 'PendingRepApproval',
    int page = 1,
    int pageSize = 20,
  }) =>
      _repo.getPendingOrders(status: status, page: page, pageSize: pageSize);
}
