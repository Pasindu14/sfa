import 'dart:async';

import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter/foundation.dart';
import 'package:uswatte/core/background/location_tracking_service.dart';
import 'package:uswatte/core/device/device_id_service.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/notifications/fcm_service.dart';
import 'package:uswatte/core/session/device_user_guard.dart';
import 'package:uswatte/core/utils/jwt_decoder.dart';
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
  final DeviceUserGuard? _deviceUserGuard;

  AuthBloc({
    required LoginUseCase loginUseCase,
    required LogoutUseCase logoutUseCase,
    required GetCurrentAuthUseCase getCurrentAuthUseCase,
    required DeviceIdService deviceIdService,
    required FcmService fcmService,
    DeviceUserGuard? deviceUserGuard,
  })  : _loginUseCase = loginUseCase,
        _logoutUseCase = logoutUseCase,
        _getCurrentAuthUseCase = getCurrentAuthUseCase,
        _deviceIdService = deviceIdService,
        _fcmService = fcmService,
        _deviceUserGuard = deviceUserGuard,
        super(const AuthInitial()) {
    on<AppStarted>(_onAppStarted);
    on<LoginSubmitted>(_onLoginSubmitted);
    on<LogoutRequested>(_onLogoutRequested);
    on<SessionExpired>(_onSessionExpired);
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
      if (token != null) {
        final userId = JwtDecoder.extractSubject(token.accessToken);
        if (userId != null) {
          try {
            await _deviceUserGuard?.recordIfUnknown(userId);
          } catch (e) {
            debugPrint('DEVICE USER RECORD ERROR: $e');
          }
        }
        emit(AuthAuthenticated(role: token.role, name: token.name));
        // Same fire-and-forget as login. Without it a user who never signs in
        // again keeps whatever token (if any) the server last saw, and misses
        // every push after the device rotates it or reinstalls the app.
        unawaited(_fcmService.registerToken());
        if (token.role == UserRole.salesRep) {
          unawaited(LocationTrackingService.start());
        }
      } else {
        emit(const AuthUnauthenticated());
      }
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
      // Must finish before AuthAuthenticated: that state mounts the home
      // screen and kicks the post-login sync, both of which read/upload the
      // local tables this may need to clear for a different user.
      final userId = JwtDecoder.extractSubject(token.accessToken);
      if (userId != null) {
        try {
          await _deviceUserGuard?.onLogin(userId);
        } catch (e, stack) {
          debugPrint('DEVICE USER GUARD ERROR: $e\n$stack');
        }
      }
      emit(AuthAuthenticated(role: token.role, name: token.name));
      // Fire-and-forget — failures never block login
      unawaited(_fcmService.registerToken());
      if (token.role == UserRole.salesRep) {
        unawaited(LocationTrackingService.start());
      }
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
    unawaited(LocationTrackingService.stop());
    emit(const AuthUnauthenticated());
  }

  /// A failed token refresh — not a choice the rep made.
  ///
  /// Credentials are cleared so the app doesn't sit in a half-authenticated
  /// state, but location tracking is deliberately left running and the queued
  /// pings are left intact: the rep is almost certainly still on their route,
  /// and killing tracking here silently loses the rest of their day. The
  /// backlog uploads as soon as they sign back in.
  Future<void> _onSessionExpired(
    SessionExpired event,
    Emitter<AuthState> emit,
  ) async {
    try {
      await _logoutUseCase();
    } catch (_) {
      // Swallow — reaching the login screen is the priority.
    }
    emit(const AuthUnauthenticated());
  }
}
