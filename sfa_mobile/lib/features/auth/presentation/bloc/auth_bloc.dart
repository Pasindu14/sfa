import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/auth/domain/usecases/get_current_auth_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/login_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/logout_usecase.dart';

part 'auth_event.dart';
part 'auth_state.dart';

class AuthBloc extends Bloc<AuthEvent, AuthState> {
  final LoginUseCase _loginUseCase;
  final LogoutUseCase _logoutUseCase;
  final GetCurrentAuthUseCase _getCurrentAuthUseCase;

  AuthBloc({
    required LoginUseCase loginUseCase,
    required LogoutUseCase logoutUseCase,
    required GetCurrentAuthUseCase getCurrentAuthUseCase,
  })  : _loginUseCase = loginUseCase,
        _logoutUseCase = logoutUseCase,
        _getCurrentAuthUseCase = getCurrentAuthUseCase,
        super(const AuthInitial()) {
    on<AppStarted>(_onAppStarted);
    on<LoginSubmitted>(_onLoginSubmitted);
    on<LogoutRequested>(_onLogoutRequested);
  }

  /// Restores session from secure storage on app start.
  /// If storage is unavailable or corrupted, falls back to unauthenticated
  /// so the user can log in fresh rather than getting a stuck splash screen.
  Future<void> _onAppStarted(
    AppStarted event,
    Emitter<AuthState> emit,
  ) async {
    try {
      final token = await _getCurrentAuthUseCase();
      emit(
        token != null ? const AuthAuthenticated() : const AuthUnauthenticated(),
      );
    } catch (_) {
      emit(const AuthUnauthenticated());
    }
  }

  Future<void> _onLoginSubmitted(
    LoginSubmitted event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());
    try {
      await _loginUseCase(username: event.username, password: event.password);
      emit(const AuthAuthenticated());
    } on AppException catch (e) {
      emit(AuthFailure(e.message));
    } catch (_) {
      emit(const AuthFailure('An unexpected error occurred.'));
    }
  }

  /// Logout is best-effort: always navigates to unauthenticated even if
  /// storage clearing fails, so the user is never stuck on the dashboard.
  Future<void> _onLogoutRequested(
    LogoutRequested event,
    Emitter<AuthState> emit,
  ) async {
    try {
      await _logoutUseCase();
    } catch (_) {
      // Swallow — navigating to login is the priority
    }
    emit(const AuthUnauthenticated());
  }
}
