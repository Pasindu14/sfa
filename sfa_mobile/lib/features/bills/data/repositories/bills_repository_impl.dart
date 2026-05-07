import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/data/models/bill_item_model.dart';
import 'package:uswatte/features/bills/data/models/bill_model.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/bills/domain/repositories/bills_repository.dart';

class BillsRepositoryImpl implements BillsRepository {
  final BillsLocalDatasource _local;
  final BillSyncService _syncService;

  const BillsRepositoryImpl(this._local, this._syncService);

  @override
  Future<Bill> createBill(Bill bill) async {
    final model = BillModel(
      clientBillId: bill.clientBillId,
      outletId: bill.outletId,
      outletName: bill.outletName,
      outletCategory: bill.outletCategory,
      billingDate: bill.billingDate,
      billDiscountRate: bill.billDiscountRate,
      subTotalAmount: bill.subTotalAmount,
      billDiscountAmount: bill.billDiscountAmount,
      totalAmount: bill.totalAmount,
      notes: bill.notes,
      latitude: bill.latitude,
      longitude: bill.longitude,
      createdAt: bill.createdAt,
      syncStatus: SyncStatus.pending,
      items: bill.items
          .map((i) => BillItemModel(
                clientBillId: i.clientBillId,
                productId: i.productId,
                quantity: i.quantity,
                unitPrice: i.unitPrice,
                discountRate: i.discountRate,
                billingItemType: i.billingItemType,
                returnType: i.returnType,
                expireDate: i.expireDate,
                lineNumber: i.lineNumber,
              ))
          .toList(),
    );
    await _local.insert(model);

    // Fire-and-forget: we don't wait for the sync result before returning,
    // so the UI can close the Create Bill page instantly. BillSyncService
    // surfaces progress via its status stream for the Bills list to subscribe to.
    _syncService.flushOne(bill.clientBillId);

    return model.toEntity();
  }

  @override
  Future<List<Bill>> getBills({int? limit}) async {
    final models = await _local.getAll(limit: limit);
    return models.map((m) => m.toEntity()).toList();
  }

  @override
  Future<Bill?> getBillById(String clientBillId) async {
    final model = await _local.getById(clientBillId);
    return model?.toEntity();
  }

  @override
  Future<int> countPendingOrFailed() => _local.countPendingOrFailed();

  @override
  Future<void> deleteLocalBill(String clientBillId) =>
      _local.delete(clientBillId);

  @override
  Future<void> retrySync(String clientBillId) => _syncService.flushOne(clientBillId);

  @override
  Future<List<ProductWithPrice>> searchProducts(
    String query, {
    int limit = 200,
    int? pricingStructureId,
  }) =>
      _local.searchProducts(query, limit: limit, pricingStructureId: pricingStructureId);
}
