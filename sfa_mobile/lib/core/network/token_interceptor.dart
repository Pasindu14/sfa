import 'dart:async';
import 'dart:io';

import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/core/device/device_id_service.dart';
import 'package:uswatte/core/env/app_env.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/session_expired_notifier.dart';
import 'package:uswatte/core/network/token_cache.dart';

/// Injects the Bearer token on every outgoing request and transparently
/// refreshes it when the server returns 401.
///
/// Refresh strategy:
///   1. On 401 (not login/refresh): attempt POST /api/v1/auth/refresh.
///   2. A [Completer] mutex ensures only one refresh runs at a time —
///      concurrent 401s wait for the same result instead of racing.
///   3. On success: update [TokenCache] + secure storage, retry original request.
///   4. On failure: fire [SessionExpiredNotifier], reject with [UnauthorizedException].
///
/// The refresh call uses a plain Dio instance (no interceptors) to avoid
/// re-entering this interceptor recursively. That one instance is created on
/// first use and reused for every refresh and every retry, so its HTTP
/// connection pool is kept instead of a new client per call.
class TokenInterceptor extends Interceptor {
  final FlutterSecureStorage _storage;
  final TokenCache _cache;
  final DeviceIdService _deviceIdService;
  final SessionExpiredNotifier _sessionExpiredNotifier;
  final Dio Function() _rawDioFactory;

  // Mutex: non-null while a refresh is in flight.
  Completer<bool>? _refreshCompleter;

  Dio? _rawDio;

  /// [rawDioFactory] exists for tests; production uses [buildRawDio].
  TokenInterceptor(
    this._storage,
    this._cache,
    this._deviceIdService,
    this._sessionExpiredNotifier, {
    @visibleForTesting Dio Function()? rawDioFactory,
  }) : _rawDioFactory = rawDioFactory ?? buildRawDio;

  /// The shared interceptor-free Dio, created lazily.
  Dio get _raw => _rawDio ??= _rawDioFactory();

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    String? token = _cache.accessToken;

    if (token == null) {
      token = await _storage.read(key: AppConstants.accessTokenKey);
      if (token != null) _cache.update(token);
    }

    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }

    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    final path = err.requestOptions.path;
    final is401 = err.response?.statusCode == 401;

    // Login/refresh 401s are handled by their own datasources — skip here.
    if (!is401 || path.contains('/auth/login') || path.contains('/auth/refresh')) {
      handler.next(err);
      return;
    }

    // ── Token expired: attempt refresh ───────────────────────────────────────
    final refreshed = await _performRefresh();

    if (refreshed) {
      // Retry the original request with the new token.
      try {
        final retryOptions = err.requestOptions;
        retryOptions.headers['Authorization'] = 'Bearer ${_cache.accessToken}';

        final response = await _raw.fetch(retryOptions);
        handler.resolve(response);
      } catch (e) {
        handler.next(err);
      }
    } else {
      handler.reject(
        DioException(
          requestOptions: err.requestOptions,
          error: const UnauthorizedException(),
          type: err.type,
          response: err.response,
        ),
      );
    }
  }

  /// Returns true if a new access token was obtained and saved.
  /// Uses a [Completer] so concurrent 401s share one refresh attempt.
  Future<bool> _performRefresh() async {
    if (_refreshCompleter != null) {
      return _refreshCompleter!.future;
    }

    _refreshCompleter = Completer<bool>();

    // The refresh token this attempt sends — kept outside the try so a rejection can
    // tell whether the stored one is still the same dead token.
    String? sentRefreshToken;
    try {
      final refreshToken =
          await _storage.read(key: AppConstants.refreshTokenKey);
      sentRefreshToken = refreshToken;
      if (refreshToken == null) {
        _fail();
        return false;
      }

      final deviceId = await _deviceIdService.getDeviceId();
      final response = await _raw.post(
        '/api/v1/auth/refresh',
        data: {'refreshToken': refreshToken, 'deviceId': deviceId},
      );

      final data =
          (response.data as Map<String, dynamic>?)?['data'] as Map<String, dynamic>?;
      final newAccess = data?['accessToken'] as String?;
      final newRefresh = data?['refreshToken'] as String?;

      if (newAccess == null) {
        _fail();
        return false;
      }

      // Persist new tokens.
      _cache.update(newAccess);
      await Future.wait([
        _storage.write(key: AppConstants.accessTokenKey, value: newAccess),
        if (newRefresh != null)
          _storage.write(key: AppConstants.refreshTokenKey, value: newRefresh),
      ]);

      _refreshCompleter!.complete(true);
      return true;
    } catch (e) {
      // The server definitively refused this refresh token (revoked / unknown / expired).
      // Left in storage it would be re-sent forever by any isolate that still holds the
      // session — notably the background location service, which keeps running with no UI
      // to log the rep out. A network error or 5xx says nothing about the token, so only
      // a 401/403 clears it.
      if (e is DioException && _isRejection(e) && sentRefreshToken != null) {
        await _dropDeadTokens(sentRefreshToken);
      }
      _fail();
      return false;
    } finally {
      _refreshCompleter = null;
    }
  }

  static bool _isRejection(DioException e) {
    final status = e.response?.statusCode;
    return status == 401 || status == 403;
  }

  /// Deletes the stored token pair, but only while the stored refresh token is still
  /// [rejected]. The UI and background isolates each run their own interceptor against the
  /// same secure storage: if the other one already rotated the pair, the stored token
  /// differs, the pair is healthy, and wiping it would log out a valid session.
  Future<void> _dropDeadTokens(String rejected) async {
    try {
      final current = await _storage.read(key: AppConstants.refreshTokenKey);
      if (current != rejected) return;
      await Future.wait([
        _storage.delete(key: AppConstants.accessTokenKey),
        _storage.delete(key: AppConstants.refreshTokenKey),
      ]);
    } catch (_) {
      // Best effort — the session-expired signal still fires.
    }
  }

  void _fail() {
    _sessionExpiredNotifier.notify();
    _cache.clear();
    _refreshCompleter?.complete(false);
  }

  /// A plain Dio with no interceptors — used exclusively for the refresh call
  /// and for retrying the original request after a successful refresh.
  static Dio buildRawDio() {
    final dio = Dio(
      BaseOptions(
        baseUrl: AppEnv.apiBaseUrl,
        connectTimeout: const Duration(seconds: 15),
        receiveTimeout: const Duration(seconds: 15),
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      ),
    );

    if (kDebugMode) {
      (dio.httpClientAdapter as IOHttpClientAdapter).createHttpClient = () {
        final client = HttpClient();
        client.badCertificateCallback = (_, __, ___) => true;
        return client;
      };
    }

    return dio;
  }
}
