import '../entities/purchase_order_detail.dart';
import '../entities/purchase_order_summary.dart';

abstract class PurchaseOrdersRepository {
  Future<List<PurchaseOrderSummary>> getPendingOrders({int page = 1, int pageSize = 20});
  Future<PurchaseOrderDetail> getOrderById(int id);
  Future<void> repApprove(int id);
  Future<void> reject(int id, String reason);
}
