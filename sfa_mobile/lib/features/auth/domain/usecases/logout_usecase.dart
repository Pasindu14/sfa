import 'package:uswatte/features/auth/domain/repositories/auth_repository.dart';

class LogoutUseCase {
  final AuthRepository _repository;

  /// Session-scoped cleanup that runs after the credentials are cleared, e.g.
  /// forgetting stored HTTP ETags so the next user never gets a 304 against
  /// the previous user's cache. Failures here never block the logout.
  final Future<void> Function()? _onLoggedOut;

  const LogoutUseCase(this._repository, {Future<void> Function()? onLoggedOut})
      : _onLoggedOut = onLoggedOut;

  Future<void> call() async {
    try {
      await _repository.logout();
    } finally {
      try {
        await _onLoggedOut?.call();
      } catch (_) {}
    }
  }
}
