import 'dart:async';

import 'package:uswatte/core/connectivity/connectivity_service.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/sync/sync_backoff.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/not_billings/data/datasources/not_billings_local_datasource.dart';
import 'package:uswatte/features/not_billings/data/datasources/not_billings_remote_datasource.dart';
import 'package:uswatte/features/not_billings/data/models/not_billing_model.dart';

class NotBillingOutboxStatus {
  final int pendingOrFailedCount;
  final String? activeClientNotBillingId;

  const NotBillingOutboxStatus({
    required this.pendingOrFailedCount,
    this.activeClientNotBillingId,
  });
}

class NotBillingSyncService {
  static const Duration retentionWindow = Duration(days: 14);

  final NotBillingsLocalDatasource _local;
  final NotBillingsRemoteDatasource _remote;
  final ConnectivityService _connectivity;

  final StreamController<NotBillingOutboxStatus> _statusCtrl =
      StreamController<NotBillingOutboxStatus>.broadcast();
  StreamSubscription<bool>? _connectivitySub;

  final Set<String> _inFlight = {};

  /// Rows another path attempted since the running flush read its batch —
  /// see BillSyncService for why.
  final Set<String> _attemptedDuringFlush = {};

  /// The flush currently running, if any. Overlapping triggers share it.
  Future<void>? _flushing;

  final DateTime Function() _now;

  NotBillingSyncService(
    this._local,
    this._remote,
    this._connectivity, {
    DateTime Function()? clock,
  }) : _now = clock ?? DateTime.now {
    _connectivitySub = _connectivity.onConnectionRestored.listen((_) {
      flushAll().catchError((_) {});
    });
  }

  Stream<NotBillingOutboxStatus> get status$ => _statusCtrl.stream;

  /// True while a [flushAll] pass is running.
  bool get isFlushing => _flushing != null;

  // Errors the rep has to resolve — retrying the same payload cannot succeed.
  // Mirrors BillSyncService._terminalErrorCodes (minus the bill-only codes).
  // DUPLICATE_NOT_BILLING: the server already holds a record for this rep +
  // outlet + day (NotBillingService.CreateAsync), so a resend is always refused.
  static const _terminalErrorCodes = {
    'VALIDATION_FAILED',
    'DUPLICATE_NOT_BILLING',
  };

  /// Attempt to sync every pending/failed row. Concurrent calls share the
  /// running flush; failed rows wait out [SyncBackoff] unless [force] is set
  /// (manual "sync now"), in which case a pass runs after any unforced one.
  Future<void> flushAll({bool force = false}) {
    final running = _flushing;
    if (running != null) {
      if (!force) return running;
      return running.then((_) => flushAll(force: true));
    }
    final future = _flushAll(force: force);
    _flushing = future;
    return future.whenComplete(() {
      if (identical(_flushing, future)) _flushing = null;
    });
  }

  Future<void> _flushAll({required bool force}) async {
    _attemptedDuringFlush.clear();
    final rows = await _local.getPendingForSync();
    for (final row in rows) {
      if (_inFlight.contains(row.clientNotBillingId)) continue;
      if (_attemptedDuringFlush.contains(row.clientNotBillingId)) continue;
      if (_terminalErrorCodes.contains(row.lastSyncErrorCode)) continue;
      if (!force && !_isDue(row)) continue;
      await _sync(row);
    }
    await _purgeOld();
    await _emitStatus();
  }

  bool _isDue(NotBillingModel row) {
    if (row.syncStatus != SyncStatus.failed) return true;
    return SyncBackoff.isDue(
      attempts: row.syncAttempts,
      lastAttemptAt: row.lastAttemptAt,
      now: _now(),
    );
  }

  /// Manual retry / fresh create — backoff does not apply, but rows failed
  /// with a terminal error code are left for the rep to resolve.
  Future<void> flushOne(String clientNotBillingId) async {
    if (_inFlight.contains(clientNotBillingId)) return;
    final row = await _local.getById(clientNotBillingId);
    if (row == null) return;
    if (row.syncStatus.dbValue == 'synced') return;
    if (row.syncStatus.dbValue == 'syncing') return;
    if (_terminalErrorCodes.contains(row.lastSyncErrorCode)) return;

    await _sync(row);
    await _emitStatus();
  }

  Future<void> _sync(NotBillingModel row) async {
    // Claim synchronously so a concurrent caller can't also send the row.
    if (!_inFlight.add(row.clientNotBillingId)) return;
    try {
      await _local.markSyncing(row.clientNotBillingId);
      _statusCtrl.add(NotBillingOutboxStatus(
        pendingOrFailedCount: await _local.countPendingOrFailed(),
        activeClientNotBillingId: row.clientNotBillingId,
      ));

      final result = await _remote.createNotBilling(row);
      await _local.markSynced(
        row.clientNotBillingId,
        serverNotBillingId: result.serverNotBillingId,
        serverNotBillingNumber: result.serverNotBillingNumber,
      );
    } on NetworkException {
      await _local.markPendingAfterNetworkError(row.clientNotBillingId);
    } on AppException catch (e) {
      await _local.markFailed(
        row.clientNotBillingId,
        errorCode: e.code,
        errorMessage: _flattenMessage(e),
      );
    } catch (e) {
      await _local.markPendingAfterNetworkError(row.clientNotBillingId);
    } finally {
      _inFlight.remove(row.clientNotBillingId);
      if (_flushing != null) {
        _attemptedDuringFlush.add(row.clientNotBillingId);
      }
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

  Future<void> _purgeOld() async {
    try {
      final cutoff = DateTime.now().toUtc().subtract(retentionWindow);
      await _local.purgeSyncedOlderThan(cutoff);
    } catch (_) {}
  }

  Future<void> _emitStatus() async {
    _statusCtrl.add(NotBillingOutboxStatus(
      pendingOrFailedCount: await _local.countPendingOrFailed(),
    ));
  }

  Future<void> dispose() async {
    await _connectivitySub?.cancel();
    await _statusCtrl.close();
  }
}
