import 'dart:async';

/// Fires a void event whenever the token refresh cycle fails and the session
/// cannot be recovered. Listeners (e.g. AuthBloc) should treat this as a
/// forced logout signal.
///
/// Registered as a lazy singleton so TokenInterceptor and main.dart share the
/// same instance without a direct reference between them.
class SessionExpiredNotifier {
  final _controller = StreamController<void>.broadcast();

  Stream<void> get stream => _controller.stream;

  void notify() => _controller.add(null);

  void dispose() => _controller.close();
}
