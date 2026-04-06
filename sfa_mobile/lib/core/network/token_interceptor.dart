import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/token_cache.dart';

/// Injects the Bearer token on every outgoing request and converts
/// 401 responses on protected endpoints into [UnauthorizedException].
///
/// Strategy:
///   1. Check in-memory [TokenCache] — zero I/O, fast path.
///   2. On cache miss (cold start before first login), fall back to
///      secure storage and warm the cache for subsequent requests.
///   3. 401 on auth endpoints (login) is left to the datasource layer.
///   4. 401 on any other endpoint means the session expired — reject
///      with [UnauthorizedException] stored in [DioException.error] so
///      datasources can unwrap it with a single `e.error is AppException`
///      guard rather than per-endpoint status code checks.
class TokenInterceptor extends Interceptor {
  final FlutterSecureStorage _storage;
  final TokenCache _cache;

  TokenInterceptor(this._storage, this._cache);

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    String? token = _cache.accessToken;

    if (token == null) {
      // Cache miss — warm from storage (happens at most once per session)
      token = await _storage.read(key: AppConstants.accessTokenKey);
      if (token != null) _cache.update(token);
    }

    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }

    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    // Login 401 = wrong credentials — let the auth datasource handle it.
    // Any other 401 = token expired — surface as UnauthorizedException so
    // all future datasources get a typed exception without extra code.
    if (err.response?.statusCode == 401 &&
        !err.requestOptions.path.contains('/auth/login')) {
      handler.reject(
        DioException(
          requestOptions: err.requestOptions,
          error: const UnauthorizedException(),
          type: err.type,
          response: err.response,
        ),
      );
      return;
    }
    handler.next(err);
  }
}
