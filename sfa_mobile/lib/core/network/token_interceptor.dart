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
/// re-entering this interceptor recursively.
class TokenInterceptor extends Interceptor {
  final FlutterSecureStorage _storage;
  final TokenCache _cache;
  final DeviceIdService _deviceIdService;
  final SessionExpiredNotifier _sessionExpiredNotifier;

  // Mutex: non-null while a refresh is in flight.
  Completer<bool>? _refreshCompleter;

  TokenInterceptor(
    this._storage,
    this._cache,
    this._deviceIdService,
    this._sessionExpiredNotifier,
  );

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

        final dio = _buildRawDio();
        final response = await dio.fetch(retryOptions);
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

    try {
      final refreshToken =
          await _storage.read(key: AppConstants.refreshTokenKey);
      if (refreshToken == null) {
        _fail();
        return false;
      }

      final deviceId = await _deviceIdService.getDeviceId();
      final dio = _buildRawDio();

      final response = await dio.post(
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
    } catch (_) {
      _fail();
      return false;
    } finally {
      _refreshCompleter = null;
    }
  }

  void _fail() {
    _sessionExpiredNotifier.notify();
    _cache.clear();
    _refreshCompleter?.complete(false);
  }

  /// A plain Dio with no interceptors — used exclusively for the refresh call
  /// and for retrying the original request after a successful refresh.
  Dio _buildRawDio() {
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
