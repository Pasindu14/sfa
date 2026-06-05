import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';

/// Application-facing contract for creating and reviewing bills.
///
/// Writes are ALWAYS local-first: `createBill` persists to SQLite, enqueues
/// for the outbox, and returns. Actual server sync is driven by
/// `BillSyncService`; the UI reflects per-row `syncStatus`.
abstract class BillsRepository {
  /// Persists [bill] to the local outbox with `syncStatus = pending` and kicks
  /// off a fire-and-forget sync attempt via [BillSyncService]. Safe offline.
  Future<Bill> createBill(Bill bill);

  Future<List<Bill>> getBills({int? limit});

  Future<Bill?> getBillById(String clientBillId);

  Future<int> countPendingOrFailed();

  /// Cancels a bill. Pending/failed bills are cancelled locally only.
  /// Synced bills also trigger PATCH /api/v1/billings/{id}/cancel on the server.
  /// Cancelled bills remain visible in the list but cannot be retried or synced.
  Future<void> cancelBill(String clientBillId);

  /// Re-triggers a sync attempt for one failed bill.
  Future<void> retrySync(String clientBillId);

  /// Product search for the Create Bill picker.
  /// Results are grouped by category (uncategorized last) in the returned list.
  /// Prices come from the product's own columns.
  Future<List<ProductWithPrice>> searchProducts(
    String query, {
    int limit = 200,
  });
}
