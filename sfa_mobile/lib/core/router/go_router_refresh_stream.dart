import 'dart:async';
import 'package:flutter/foundation.dart';

/// Converts a BLoC/Stream into a [ChangeNotifier] that go_router can
/// listen to via [GoRouter.refreshListenable].
/// When the stream emits, go_router re-evaluates all [redirect] callbacks.
class GoRouterRefreshStream extends ChangeNotifier {
  GoRouterRefreshStream(Stream<dynamic> stream) {
    notifyListeners();
    _subscription = stream.asBroadcastStream().listen((_) => notifyListeners());
  }

  late final StreamSubscription<dynamic> _subscription;

  @override
  void dispose() {
    _subscription.cancel();
    super.dispose();
  }
}
