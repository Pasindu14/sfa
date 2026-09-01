part of 'auth_bloc.dart';

abstract class AuthEvent extends Equatable {
  const AuthEvent();

  @override
  List<Object?> get props => [];
}

/// Fired on app start to restore session from secure storage.
class AppStarted extends AuthEvent {
  const AppStarted();
}

class LoginSubmitted extends AuthEvent {
  final String username;
  final String password;

  const LoginSubmitted({required this.username, required this.password});

  @override
  List<Object?> get props => [username, password];
}

/// The rep deliberately logged out. Ends location tracking.
class LogoutRequested extends AuthEvent {
  const LogoutRequested();
}

/// The token refresh cycle failed and the session could not be recovered.
///
/// Distinct from [LogoutRequested] on purpose: the rep did not choose this and
/// is very likely still mid-route, so location tracking keeps running and the
/// queued pings are kept. They upload once the rep signs back in.
class SessionExpired extends AuthEvent {
  const SessionExpired();
}
