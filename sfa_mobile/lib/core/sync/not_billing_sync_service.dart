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

  /// A row left in `syncing` longer than this has lost its sender and is put
  /// back to `pending`. See BillSyncService.syncingStaleAfter.
  static const Duration syncingStaleAfter = Duration(minutes: 2);

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

  /// Puts rows stuck in `syncing` back to `pending` so they are counted and
  /// re-sent — see BillSyncService.recoverStuckRows. Never throws.
  Future<int> recoverStuckRows() async {
    try {
      final reset = await _local.resetStaleSyncing(
        staleBefore: _now().subtract(syncingStaleAfter),
        inFlightIds: Set.of(_inFlight),
      );
      if (reset > 0) await _emitStatus();
      return reset;
    } catch (_) {
      return 0;
    }
  }

  Future<void> _flushAll({required bool force}) async {
    _attemptedDuringFlush.clear();
    await recoverStuckRows();
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
    var row = await _local.getById(clientNotBillingId);
    if (row == null) return;
    if (row.syncStatus == SyncStatus.syncing) {
      // A manual retry on a row whose sender died: recover it if stale,
      // otherwise somebody is genuinely sending it.
      if (await recoverStuckRows() == 0) return;
      row = await _local.getById(clientNotBillingId);
      if (row == null) return;
    }
    if (row.syncStatus == SyncStatus.synced ||
        row.syncStatus == SyncStatus.syncing) {
      return;
    }
    if (_terminalErrorCodes.contains(row.lastSyncErrorCode)) return;

    await _sync(row);
    await _emitStatus();
  }

  /// Deletes an unsynced visit from the device. Refused while the row is
  /// being sent — deleting under an in-flight POST would leave a visit on the
  /// server that the phone no longer shows. A stale `syncing` claim (its
  /// sender died) is not "being sent". Throws an [AppException] with a
  /// rep-friendly message when refused.
  Future<void> deleteOne(String clientNotBillingId) async {
    final row = await _local.getById(clientNotBillingId);
    if (row == null) return;
    final claimedAt = row.lastAttemptAt;
    final claimIsLive = row.syncStatus == SyncStatus.syncing &&
        claimedAt != null &&
        !claimedAt.isBefore(_now().subtract(syncingStaleAfter));
    if (_inFlight.contains(clientNotBillingId) || claimIsLive) {
      throw const BusinessRuleException(
        code: 'NOT_BILLING_SYNC_IN_PROGRESS',
        message: 'This visit is being sent right now. Try again in a moment.',
      );
    }
    await _local.delete(clientNotBillingId);
    await _emitStatus();
  }

  Future<void> _sync(NotBillingModel row) async {
    // Claim synchronously so a concurrent caller can't also send the row.
    if (!_inFlight.add(row.clientNotBillingId)) return;
    try {
      // Second, cross-isolate claim: only one sender wins the DB transition
      // pending/failed -> syncing.
      if (!await _local.claimForSync(row.clientNotBillingId, now: _now())) {
        return;
      }
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
