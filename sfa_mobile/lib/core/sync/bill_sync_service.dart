import 'dart:async';

import 'package:uswatte/core/connectivity/connectivity_service.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/sync/sync_backoff.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/data/datasources/bills_remote_datasource.dart';
import 'package:uswatte/features/bills/data/models/bill_model.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/outlets/data/datasources/outlets_local_datasource.dart';
import 'package:uswatte/features/stock/domain/usecases/sync_distributor_stock_usecase.dart';

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

  /// A row left in `syncing` longer than this has lost its sender (killed
  /// process, torn-down isolate) and is put back to `pending`. Deliberately
  /// longer than any request can run (15s connect + 15s receive), so a claim
  /// held by another isolate is never reset out from under it.
  static const Duration syncingStaleAfter = Duration(minutes: 2);

  final BillsLocalDatasource _local;
  final BillsRemoteDatasource _remote;
  final ConnectivityService _connectivity;
  final SyncDistributorStockUseCase _syncStock;
  final OutletsLocalDatasource _outletsLocal;

  final StreamController<BillOutboxStatus> _statusCtrl =
      StreamController<BillOutboxStatus>.broadcast();
  StreamSubscription<bool>? _connectivitySub;

  /// Set of client IDs currently in-flight. Prevents a second trigger from
  /// re-sending a row that's still being posted.
  final Set<String> _inFlight = {};

  /// Rows another path attempted since the running flush read its batch. A
  /// [flushOne] (new bill / manual retry) can finish a row while flushAll is
  /// still working through a stale list that holds it as pending.
  final Set<String> _attemptedDuringFlush = {};

  /// The flush currently running, if any. Overlapping triggers share it rather
  /// than reading the same pending rows and POSTing them a second time.
  Future<void>? _flushing;

  final DateTime Function() _now;

  BillSyncService(
    this._local,
    this._remote,
    this._connectivity,
    this._syncStock,
    this._outletsLocal, {
    DateTime Function()? clock,
  }) : _now = clock ?? DateTime.now {
    _connectivitySub = _connectivity.onConnectionRestored.listen((_) {
      // Fire-and-forget: swallow errors so the listener stays alive.
      flushAll().catchError((_) {});
    });
  }

  Stream<BillOutboxStatus> get status$ => _statusCtrl.stream;

  /// True while a [flushAll] pass is running.
  bool get isFlushing => _flushing != null;

  /// Attempt to sync every pending/failed row. Safe to call concurrently — a
  /// call made while a flush is running returns that flush's future, and rows
  /// already in-flight are skipped. Failed rows wait out [SyncBackoff] unless
  /// [force] is set (manual "sync now"); a forced call that lands during an
  /// unforced flush runs its own pass once that one finishes.
  ///
  /// Distributor stock is refreshed once at the end if at least one bill
  /// synced. Opportunistically purges synced bills older than
  /// [retentionWindow] so the local DB stays bounded.
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

  /// Puts rows stuck in `syncing` back to `pending` so they show up in the
  /// pending badge / logout check and get re-sent. Skips rows this isolate is
  /// sending right now and rows claimed within [syncingStaleAfter] (another
  /// isolate may be mid-request). Safe to re-send: the client bill id is the
  /// server's idempotency key. Never throws — recovery must not block a flush.
  /// Returns how many rows were reset.
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
    var synced = 0;
    for (final row in rows) {
      if (_inFlight.contains(row.clientBillId)) continue;
      if (_attemptedDuringFlush.contains(row.clientBillId)) continue;
      if (_terminalErrorCodes.contains(row.lastSyncErrorCode)) continue;
      if (!force && !_isDue(row)) continue;
      if (await _sync(row)) synced++;
    }
    if (synced > 0) await _refreshStock();
    await _purgeOld();
    await _emitStatus();
  }

  bool _isDue(BillModel row) {
    if (row.syncStatus != SyncStatus.failed) return true;
    return SyncBackoff.isDue(
      attempts: row.syncAttempts,
      lastAttemptAt: row.lastAttemptAt,
      now: _now(),
    );
  }

  /// Refresh local stock after bills synced — best-effort, never blocks. Forced
  /// because the server's counts just changed, so a recent sync is stale.
  Future<void> _refreshStock() async {
    try {
      await _syncStock(force: true);
    } catch (_) {}
  }

  /// Pulls the rep's own bills down from the server into the local store.
  ///
  /// This is the counterpart to [flushAll] — without it the local store is write-only, so a
  /// reinstall or a new phone leaves the bill list empty even though the server still holds
  /// every bill. Upload runs first: a pending row that is about to sync should not be shadowed
  /// by a downloaded copy of the same bill.
  ///
  /// Unsynced rows are never overwritten (see [BillsLocalDatasource.upsertFromServer]).
  /// Returns how many local rows were written.
  Future<int> downloadMyBills({int days = 7}) async {
    if (!await _connectivity.hasInternet()) {
      throw const NetworkException(message: 'No internet connection.');
    }

    // Push before pulling, so anything still queued locally wins its own row.
    // This is the Sync page's explicit action, so it skips retry backoff.
    await flushAll(force: true);

    final bills = await _remote.fetchMyBillsForSync(days: days);
    final written = await _local.upsertFromServer(bills);
    await _emitStatus();
    return written;
  }

  Future<void> _purgeOld() async {
    try {
      final cutoff = DateTime.now().toUtc().subtract(retentionWindow);
      await _local.purgeSyncedOlderThan(cutoff);
    } catch (_) {
      // Purge is best-effort; a failure here must never block syncing.
    }
  }

  // Geofence rejections are terminal: the server refused this bill for where it
  // was taken, and retrying the same stored coordinates can never succeed. Left
  // out of this set they would retry until the outbox gave up.
  // Keep in sync with _ActionRow._terminalCodes in bill_detail_page.dart.
  static const _terminalErrorCodes = {
    'INSUFFICIENT_STOCK',
    'VALIDATION_FAILED',
    'OUTLET_OUT_OF_RANGE',
    'BILLING_LOCATION_REQUIRED',
  };

  /// Attempt to sync one row by its client ID. No-ops if the row doesn't exist,
  /// is already synced, in-flight, or failed with a terminal error code that
  /// the rep must resolve manually (e.g. out of stock, validation failure).
  /// Used for a fresh create and the manual retry button, so retry backoff does
  /// not apply. Refreshes distributor stock on success.
  Future<void> flushOne(String clientBillId) async {
    if (_inFlight.contains(clientBillId)) return;
    var row = await _local.getById(clientBillId);
    if (row == null) return;
    if (row.syncStatus == SyncStatus.syncing) {
      // A manual retry on a row whose sender died: recover it if it is stale,
      // otherwise somebody is genuinely sending it.
      if (await recoverStuckRows() == 0) return;
      row = await _local.getById(clientBillId);
      if (row == null) return;
    }
    if (row.syncStatus == SyncStatus.synced ||
        row.syncStatus == SyncStatus.syncing ||
        row.syncStatus == SyncStatus.cancelled) {
      return;
    }
    if (_terminalErrorCodes.contains(row.lastSyncErrorCode)) return;

    if (await _sync(row)) await _refreshStock();
    await _emitStatus();
  }

  /// Cancels (deletes) a bill.
  ///
  /// Synced bills: best-effort PATCH cancel — network failures are swallowed
  /// so the local row is always marked cancelled.
  ///
  /// Unsynced bills the server may nevertheless hold (an attempt was made but
  /// no confirmation came back, so the response was lost): the server is asked
  /// first. Found → cancelled there too (unless it already is); 404 → it never
  /// arrived, cancel locally. If the server cannot be asked, or is mid-send,
  /// the delete is refused by throwing an [AppException] with a rep-friendly
  /// message — deleting locally would leave a live bill on the server that the
  /// phone no longer shows.
  Future<void> cancelOne(String clientBillId) async {
    final row = await _local.getById(clientBillId);
    if (row == null || row.syncStatus == SyncStatus.cancelled) return;

    if (row.serverBillId != null) {
      try {
        await _remote.cancelBilling(row.serverBillId!);
      } catch (_) {
        // Network or server error — still cancel locally.
      }
      await _local.markCancelled(clientBillId);
      await _emitStatus();
      return;
    }

    // Claim the id so a concurrent flush cannot send the row while the server
    // is being asked about it (and so a delete cannot start mid-send).
    if (_isBeingSent(row) || !_inFlight.add(clientBillId)) {
      throw _sendInProgress;
    }
    try {
      if (_wasAttempted(row)) await _cancelOnServerIfPresent(row);
      await _local.markCancelled(clientBillId);
    } finally {
      _inFlight.remove(clientBillId);
    }
    await _emitStatus();
  }

  static const _sendInProgress = BusinessRuleException(
    code: 'BILL_SYNC_IN_PROGRESS',
    message: 'This bill is being sent right now. Try again in a moment.',
  );

  static const _needsInternet = NetworkException(
    message: 'Connect to the internet to delete this bill.',
  );

  /// In flight in this isolate, or claimed so recently another isolate may be
  /// sending it. A stale `syncing` claim is not "being sent" — its sender is gone.
  bool _isBeingSent(BillModel row) {
    if (_inFlight.contains(row.clientBillId)) return true;
    if (row.syncStatus != SyncStatus.syncing) return false;
    final claimedAt = row.lastAttemptAt;
    return claimedAt != null &&
        !claimedAt.isBefore(_now().subtract(syncingStaleAfter));
  }

  /// Whether a POST for this row may have reached the server: any recorded
  /// attempt, or a claim that never got a result.
  bool _wasAttempted(BillModel row) =>
      row.syncAttempts > 0 ||
      row.lastAttemptAt != null ||
      row.syncStatus == SyncStatus.syncing;

  Future<void> _cancelOnServerIfPresent(BillModel row) async {
    final ServerBillLookup? found;
    try {
      found = await _remote.findByClientBillId(row.clientBillId);
    } on NetworkException {
      throw _needsInternet;
    } on AppException {
      throw const BusinessRuleException(
        code: 'BILL_LOOKUP_FAILED',
        message:
            "We couldn't check this bill with the server. Please try again in a moment.",
      );
    }
    // The server never received it — nothing to undo there.
    if (found == null || found.isCancelled) return;

    try {
      await _remote.cancelBilling(found.id);
    } on NetworkException {
      throw _needsInternet;
    } on AppException catch (e) {
      final refused = e is BusinessRuleException ||
          e is ConflictException ||
          e is ValidationException;
      if (!refused) {
        throw const BusinessRuleException(
          code: 'BILL_CANCEL_FAILED',
          message:
              "We couldn't delete this bill right now. Please try again in a moment.",
        );
      }
      // The server holds a live bill it will not cancel (e.g. already
      // approved). Show it as the synced bill it is rather than hiding it.
      await _local.markSynced(
        row.clientBillId,
        serverBillId: found.id,
        serverBillNumber: found.billingNumber ?? '',
      );
      await _emitStatus();
      throw const BusinessRuleException(
        code: 'BILL_ALREADY_RECEIVED',
        message:
            'This bill had already reached the server and can no longer be deleted here. It now shows as synced.',
      );
    }
  }

  /// Sends one row. Returns true when the server accepted it.
  Future<bool> _sync(BillModel row) async {
    // Claim synchronously (no await before this) so a concurrent caller that
    // checked _inFlight a moment earlier can't also send the row.
    if (!_inFlight.add(row.clientBillId)) return false;
    try {
      // Second, cross-isolate claim: only one sender wins the DB transition
      // pending/failed -> syncing, so the WorkManager isolate and this one
      // cannot both POST the row.
      if (!await _local.claimForSync(row.clientBillId, now: _now())) {
        return false;
      }
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

      // Stamp local outlet so the "NEW" badge clears immediately — best-effort.
      try {
        await _outletsLocal.stampLastBillDate(row.outletId, row.billingDate);
      } catch (_) {}

      // Stock is refreshed by the caller — once per flushAll, not per bill.
      return true;
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
      if (_flushing != null) _attemptedDuringFlush.add(row.clientBillId);
    }
    return false;
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
