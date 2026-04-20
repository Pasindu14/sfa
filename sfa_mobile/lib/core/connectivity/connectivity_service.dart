import 'dart:async';

import 'package:connectivity_plus/connectivity_plus.dart';

/// Thin wrapper over `connectivity_plus`.
///
/// The outbox (BillSyncService) subscribes to [onConnectionRestored] so any
/// pending bills are flushed the moment the phone reconnects after being
/// offline. A hard online check via [hasInternet] is used at POST time to
/// avoid burning a Dio timeout when airplane mode is on.
class ConnectivityService {
  final Connectivity _connectivity;
  final StreamController<bool> _restoredCtrl = StreamController<bool>.broadcast();
  StreamSubscription<List<ConnectivityResult>>? _sub;
  bool _lastOnline = true;

  ConnectivityService({Connectivity? connectivity})
      : _connectivity = connectivity ?? Connectivity() {
    _sub = _connectivity.onConnectivityChanged.listen(_handleChange);
  }

  /// True when at least one non-`none` connectivity result is present.
  Future<bool> hasInternet() async {
    final results = await _connectivity.checkConnectivity();
    return _isOnline(results);
  }

  /// Emits `true` each time connectivity transitions from offline → online.
  /// Does NOT emit on startup or on online → online changes.
  Stream<bool> get onConnectionRestored => _restoredCtrl.stream;

  void _handleChange(List<ConnectivityResult> results) {
    final online = _isOnline(results);
    if (online && !_lastOnline) {
      _restoredCtrl.add(true);
    }
    _lastOnline = online;
  }

  bool _isOnline(List<ConnectivityResult> results) =>
      results.any((r) => r != ConnectivityResult.none);

  Future<void> dispose() async {
    await _sub?.cancel();
    await _restoredCtrl.close();
  }
}
