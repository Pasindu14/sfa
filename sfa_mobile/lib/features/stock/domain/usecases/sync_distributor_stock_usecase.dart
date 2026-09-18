import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/stock/data/datasources/distributor_stock_local_datasource.dart';
import 'package:uswatte/features/stock/data/datasources/distributor_stock_remote_datasource.dart';
import 'package:uswatte/features/stock/data/models/distributor_stock_model.dart';

/// Downloads the distributor stock snapshot and replaces the local table.
///
/// Several triggers ask for this close together (connectivity restore, app
/// resume, bill flush, background sync, the Sync page), and each call is a full
/// download + table replace. So:
///   - concurrent calls share the one in-flight download (a forced call waits
///     for it, then downloads again);
///   - a call within [minInterval] of the last successful sync is skipped,
///     unless [force] is set (explicit user refresh, or bills just synced and
///     changed the server's counts).
class SyncDistributorStockUseCase {
  static const Duration minInterval = Duration(seconds: 60);

  final DistributorStockRemoteDatasource _remote;
  final DistributorStockLocalDatasource _local;
  final DateTime Function() _now;

  Future<void>? _inFlight;
  DateTime? _lastSuccessAt;

  SyncDistributorStockUseCase(
    this._remote,
    this._local, {
    DateTime Function()? clock,
  }) : _now = clock ?? DateTime.now;

  Future<void> call({bool force = false}) {
    final running = _inFlight;
    if (running != null) {
      if (!force) return running;
      // The running download may have started before whatever prompted the
      // force (e.g. a bill that just synced), so fetch again once it ends.
      return running
          .then<void>((_) {}, onError: (Object _) {})
          .then((_) => call(force: true));
    }

    final last = _lastSuccessAt;
    if (!force && last != null && _now().difference(last) < minInterval) {
      return Future.value();
    }

    final future = _run();
    _inFlight = future;
    return future.whenComplete(() {
      if (identical(_inFlight, future)) _inFlight = null;
    });
  }

  Future<void> _run() async {
    final List<DistributorStockModel> stocks;
    try {
      stocks = await _remote.fetchAll();
    } on BusinessRuleException catch (e) {
      // The rep has no distributor (anymore). The server's answer is authoritative, so drop the
      // old snapshot — otherwise the picker keeps showing another distributor's stock forever.
      if (e.code == 'NO_DISTRIBUTOR_ASSIGNED') await _local.replaceAll(const []);
      rethrow;
    }
    await _local.replaceAll(stocks);
    final now = _now();
    await _local.saveLastSyncedAt(now);
    _lastSuccessAt = now;
  }
}
