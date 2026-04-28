import 'dart:async';
import 'package:flutter/scheduler.dart';
import 'package:flutter/widgets.dart';

/// Converts a BLoC/Stream into a [ChangeNotifier] that go_router can
/// listen to via [GoRouter.refreshListenable].
/// When the stream emits, go_router re-evaluates all [redirect] callbacks.
///
/// Notifications are guarded against firing during the build/layout/paint
/// phases. If we're between frames or already past post-frame callbacks,
/// notify synchronously; otherwise defer to after the current frame so we
/// never call markNeedsBuild on the Router while it (or any descendant such
/// as HeroControllerScope) is still building.
class GoRouterRefreshStream extends ChangeNotifier {
  GoRouterRefreshStream(Stream<dynamic> stream) {
    _subscription = stream.asBroadcastStream().listen((_) => _safeNotify());
  }

  late final StreamSubscription<dynamic> _subscription;

  void _safeNotify() {
    final phase = SchedulerBinding.instance.schedulerPhase;
    if (phase == SchedulerPhase.idle ||
        phase == SchedulerPhase.postFrameCallbacks) {
      notifyListeners();
    } else {
      WidgetsBinding.instance.addPostFrameCallback((_) => notifyListeners());
    }
  }

  @override
  void dispose() {
    _subscription.cancel();
    super.dispose();
  }
}
