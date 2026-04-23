import 'package:uswatte/core/sync/not_billing_sync_service.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/not_billings/data/datasources/not_billings_local_datasource.dart';
import 'package:uswatte/features/not_billings/data/models/not_billing_model.dart';
import 'package:uswatte/features/not_billings/domain/entities/not_billing.dart';
import 'package:uswatte/features/not_billings/domain/repositories/not_billings_repository.dart';

class NotBillingsRepositoryImpl implements NotBillingsRepository {
  final NotBillingsLocalDatasource _local;
  final NotBillingSyncService _syncService;

  const NotBillingsRepositoryImpl(this._local, this._syncService);

  @override
  Future<NotBilling> createNotBilling(NotBilling record) async {
    final model = NotBillingModel(
      clientNotBillingId: record.clientNotBillingId,
      outletId: record.outletId,
      outletName: record.outletName,
      routeName: record.routeName,
      notBillingDate: record.notBillingDate,
      reason: record.reason,
      notes: record.notes,
      createdAt: record.createdAt,
      syncStatus: SyncStatus.pending,
    );
    await _local.insert(model);

    // Fire-and-forget: page closes instantly; sync service handles the POST.
    _syncService.flushOne(record.clientNotBillingId);

    return model.toEntity();
  }

  @override
  Future<List<NotBilling>> getNotBillings({int? limit}) async {
    final models = await _local.getAll(limit: limit);
    return models.map((m) => m.toEntity()).toList();
  }

  @override
  Future<NotBilling?> getNotBillingById(String clientNotBillingId) async {
    final model = await _local.getById(clientNotBillingId);
    return model?.toEntity();
  }

  @override
  Future<int> countPendingOrFailed() => _local.countPendingOrFailed();

  @override
  Future<void> deleteLocalNotBilling(String clientNotBillingId) =>
      _local.delete(clientNotBillingId);

  @override
  Future<void> retrySync(String clientNotBillingId) =>
      _syncService.flushOne(clientNotBillingId);
}
