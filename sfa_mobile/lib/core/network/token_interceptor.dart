import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/core/network/token_cache.dart';

/// Injects the Bearer token on every outgoing request.
///
/// Strategy:
///   1. Check in-memory [TokenCache] — zero I/O, fast path.
///   2. On cache miss (cold start before first login), fall back to
///      secure storage and warm the cache for subsequent requests.
///   3. 401 handling is left to the datasource layer.
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
}
