import '../../domain/entities/purchase_order_detail.dart';
import '../../domain/entities/purchase_order_summary.dart';
import '../../domain/repositories/purchase_orders_repository.dart';
import '../datasources/purchase_orders_remote_datasource.dart';

class PurchaseOrdersRepositoryImpl implements PurchaseOrdersRepository {
  final PurchaseOrdersRemoteDatasource _remote;
  const PurchaseOrdersRepositoryImpl(this._remote);

  @override
  Future<List<PurchaseOrderSummary>> getPendingOrders({
    int page = 1,
    int pageSize = 20,
  }) =>
      _remote.getPendingOrders(page: page, pageSize: pageSize);

  @override
  Future<PurchaseOrderDetail> getOrderById(int id) => _remote.getOrderById(id);

  @override
  Future<void> repApprove(int id) => _remote.repApprove(id);

  @override
  Future<void> reject(int id, String reason) => _remote.reject(id, reason);
}
