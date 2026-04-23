import 'dart:async';

import 'package:uswatte/core/connectivity/connectivity_service.dart';
import 'package:uswatte/core/errors/app_exception.dart';
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

  NotBillingSyncService(this._local, this._remote, this._connectivity) {
    _connectivitySub = _connectivity.onConnectionRestored.listen((_) {
      flushAll();
    });
  }

  Stream<NotBillingOutboxStatus> get status$ => _statusCtrl.stream;

  Future<void> flushAll() async {
    final rows = await _local.getPendingForSync();
    for (final row in rows) {
      await _sync(row);
    }
    await _purgeOld();
    await _emitStatus();
  }

  Future<void> flushOne(String clientNotBillingId) async {
    if (_inFlight.contains(clientNotBillingId)) return;
    final row = await _local.getById(clientNotBillingId);
    if (row == null) return;
    if (row.syncStatus.dbValue == 'synced') return;
    if (row.syncStatus.dbValue == 'syncing') return;

    await _sync(row);
    await _emitStatus();
  }

  Future<void> _sync(NotBillingModel row) async {
    _inFlight.add(row.clientNotBillingId);
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
