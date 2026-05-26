import 'dart:async';

import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter/foundation.dart';
import 'package:uswatte/core/device/device_id_service.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/notifications/fcm_service.dart';
import 'package:uswatte/features/auth/domain/entities/user_role.dart';
import 'package:uswatte/features/auth/domain/usecases/get_current_auth_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/login_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/logout_usecase.dart';

part 'auth_event.dart';
part 'auth_state.dart';

class AuthBloc extends Bloc<AuthEvent, AuthState> {
  final LoginUseCase _loginUseCase;
  final LogoutUseCase _logoutUseCase;
  final GetCurrentAuthUseCase _getCurrentAuthUseCase;
  final DeviceIdService _deviceIdService;
  final FcmService _fcmService;

  AuthBloc({
    required LoginUseCase loginUseCase,
    required LogoutUseCase logoutUseCase,
    required GetCurrentAuthUseCase getCurrentAuthUseCase,
    required DeviceIdService deviceIdService,
    required FcmService fcmService,
  })  : _loginUseCase = loginUseCase,
        _logoutUseCase = logoutUseCase,
        _getCurrentAuthUseCase = getCurrentAuthUseCase,
        _deviceIdService = deviceIdService,
        _fcmService = fcmService,
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
        token != null
            ? AuthAuthenticated(role: token.role, name: token.name)
            : const AuthUnauthenticated(),
      );
    } catch (e, stack) {
      debugPrint('AUTH RESTORE ERROR: $e\n$stack');
      emit(const AuthUnauthenticated());
    }
  }

  Future<void> _onLoginSubmitted(
    LoginSubmitted event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());
    try {
      final deviceId = await _deviceIdService.getDeviceId();
      final token = await _loginUseCase(
        username: event.username,
        password: event.password,
        deviceId: deviceId,
      );
      emit(AuthAuthenticated(role: token.role, name: token.name));
      // Fire-and-forget — FCM failure never blocks login
      unawaited(_fcmService.registerToken());
    } on AppException catch (e) {
      emit(AuthFailure(e.message));
    } catch (e, stack) {
      debugPrint('LOGIN ERROR: $e\n$stack');
      emit(AuthFailure(kDebugMode ? e.toString() : 'An unexpected error occurred.'));
    }
  }

  /// Logout is best-effort: always navigates to unauthenticated even if
  /// storage clearing fails, so the user is never stuck on the dashboard.
  Future<void> _onLogoutRequested(
    LogoutRequested event,
    Emitter<AuthState> emit,
  ) async {
    try {
      // Clear FCM token first while auth token is still valid
      await _fcmService.clearToken();
      await _logoutUseCase();
    } catch (_) {
      // Swallow — navigating to login is the priority
    }
    emit(const AuthUnauthenticated());
  }
}
