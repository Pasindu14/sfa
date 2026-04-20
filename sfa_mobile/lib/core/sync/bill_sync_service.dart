import 'dart:async';

import 'package:uswatte/core/connectivity/connectivity_service.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/data/datasources/bills_remote_datasource.dart';
import 'package:uswatte/features/bills/data/models/bill_model.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';

/// Emitted by [BillSyncService] whenever its view of the outbox changes.
/// The Bills list + home-tab badge listen to this so their UI refreshes
/// without the user manually pulling to reload.
class BillOutboxStatus {
  /// Total rows where sync_status IN ('pending', 'failed').
  final int pendingOrFailedCount;

  /// Client ID of the row currently being sent, or null if idle.
  final String? activeClientBillId;

  const BillOutboxStatus({
    required this.pendingOrFailedCount,
    this.activeClientBillId,
  });
}

/// Processes the local bills outbox.
///
/// Triggers (any one of these kicks a flush):
///   1. [flushOne] — called immediately after a local create or a manual retry.
///   2. Connectivity restore — subscribed via [ConnectivityService].
///   3. App lifecycle resume — wired from `main.dart` with a
///      WidgetsBindingObserver.
///   4. Manual "Sync Bills" button on the Sync page.
///
/// Error routing:
///   - [NetworkException] / Dio connection errors → row stays `pending`,
///     attempts++, retry later.
///   - Any other `AppException` → row flips to `failed`, error message stored
///     in last_sync_error for the UI to display.
class BillSyncService {
  /// How long synced bills are kept on the device before being purged.
  /// The server remains the system of record; anything older is still
  /// retrievable via GET /api/v1/billings when the rep needs it.
  static const Duration retentionWindow = Duration(days: 14);

  final BillsLocalDatasource _local;
  final BillsRemoteDatasource _remote;
  final ConnectivityService _connectivity;

  final StreamController<BillOutboxStatus> _statusCtrl =
      StreamController<BillOutboxStatus>.broadcast();
  StreamSubscription<bool>? _connectivitySub;

  /// Set of client IDs currently in-flight. Prevents a second trigger from
  /// re-sending a row that's still being posted.
  final Set<String> _inFlight = {};

  BillSyncService(this._local, this._remote, this._connectivity) {
    _connectivitySub = _connectivity.onConnectionRestored.listen((_) {
      // Fire-and-forget: swallow errors so the listener stays alive.
      flushAll();
    });
  }

  Stream<BillOutboxStatus> get status$ => _statusCtrl.stream;

  /// Attempt to sync every pending/failed row. Safe to call concurrently —
  /// rows already in-flight are skipped. Opportunistically purges synced bills
  /// older than [retentionWindow] at the end so the local DB stays bounded.
  Future<void> flushAll() async {
    final rows = await _local.getPendingForSync();
    for (final row in rows) {
      await _sync(row);
    }
    await _purgeOld();
    await _emitStatus();
  }

  Future<void> _purgeOld() async {
    try {
      final cutoff = DateTime.now().toUtc().subtract(retentionWindow);
      await _local.purgeSyncedOlderThan(cutoff);
    } catch (_) {
      // Purge is best-effort; a failure here must never block syncing.
    }
  }

  /// Attempt to sync one row by its client ID. No-ops if the row doesn't exist,
  /// is already synced, or is currently in-flight.
  Future<void> flushOne(String clientBillId) async {
    if (_inFlight.contains(clientBillId)) return;
    final row = await _local.getById(clientBillId);
    if (row == null) return;
    if (row.syncStatus.dbValue == 'synced') return;
    if (row.syncStatus.dbValue == 'syncing') return;

    await _sync(row);
    await _emitStatus();
  }

  Future<void> _sync(BillModel row) async {
    _inFlight.add(row.clientBillId);
    try {
      await _local.markSyncing(row.clientBillId);
      _statusCtrl.add(BillOutboxStatus(
        pendingOrFailedCount: await _local.countPendingOrFailed(),
        activeClientBillId: row.clientBillId,
      ));

      final result = await _remote.createBilling(row);
      await _local.markSynced(
        row.clientBillId,
        serverBillId: result.serverBillId,
        serverBillNumber: result.serverBillNumber,
      );
    } on NetworkException {
      // Reversible: stay pending, try again on next trigger.
      await _local.markPendingAfterNetworkError(row.clientBillId);
    } on AppException catch (e) {
      // Terminal: needs rep action. Surface the code + message for UI.
      await _local.markFailed(
        row.clientBillId,
        errorCode: e.code,
        errorMessage: _flattenMessage(e),
      );
    } catch (e) {
      // Unknown failure — treat as transient so nothing is lost.
      await _local.markPendingAfterNetworkError(row.clientBillId);
    } finally {
      _inFlight.remove(row.clientBillId);
    }
  }

  String _flattenMessage(AppException e) {
    if (e is BusinessRuleException && (e.detail?.isNotEmpty ?? false)) {
      return e.detail!;
    }
    if (e is ValidationException && e.fields.isNotEmpty) {
      return e.fields.values.expand((v) => v).join('\n');
    }
    return e.message;
  }

  Future<void> _emitStatus() async {
    _statusCtrl.add(BillOutboxStatus(
      pendingOrFailedCount: await _local.countPendingOrFailed(),
    ));
  }

  Future<void> dispose() async {
    await _connectivitySub?.cancel();
    await _statusCtrl.close();
  }
}
