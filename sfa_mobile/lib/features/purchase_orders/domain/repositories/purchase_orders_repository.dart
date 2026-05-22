import '../entities/editable_order_item.dart';
import '../entities/product_with_price.dart';
import '../entities/purchase_order_detail.dart';
import '../entities/purchase_order_summary.dart';

abstract class PurchaseOrdersRepository {
  Future<List<PurchaseOrderSummary>> getPendingOrders({
    String status = 'PendingRepApproval',
    int page = 1,
    int pageSize = 20,
  });
  Future<PurchaseOrderDetail> getOrderById(int id);
  Future<void> repApprove(int id);
  Future<void> managerApprove(int id);
  Future<void> reject(int id, String reason);
  Future<void> updateOrder(int id, List<EditableOrderItem> items, String? notes);
  Future<List<ProductWithPrice>> getProductsForDistributor(int distributorId);
}
