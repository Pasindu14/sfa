import '../entities/editable_order_item.dart';
import '../repositories/purchase_orders_repository.dart';

class UpdatePurchaseOrderUseCase {
  final PurchaseOrdersRepository _repo;
  const UpdatePurchaseOrderUseCase(this._repo);

  Future<void> call(int id, List<EditableOrderItem> items, String? notes) =>
      _repo.updateOrder(id, items, notes);
}
