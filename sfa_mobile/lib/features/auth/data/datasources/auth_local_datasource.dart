import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/core/network/token_cache.dart';
import 'package:uswatte/features/auth/domain/entities/auth_token.dart';

class AuthLocalDatasource {
  final FlutterSecureStorage _storage;
  final TokenCache _cache;

  const AuthLocalDatasource(this._storage, this._cache);

  Future<void> saveToken(AuthToken token) async {
    // Update cache immediately so TokenInterceptor picks it up on next request
    _cache.update(token.accessToken);

    // Write both keys in parallel
    await Future.wait([
      _storage.write(
          key: AppConstants.accessTokenKey, value: token.accessToken),
      if (token.refreshToken != null)
        _storage.write(
            key: AppConstants.refreshTokenKey, value: token.refreshToken!),
    ]);
  }

  Future<AuthToken?> getToken() async {
    // Read both keys in parallel
    final results = await Future.wait([
      _storage.read(key: AppConstants.accessTokenKey),
      _storage.read(key: AppConstants.refreshTokenKey),
    ]);

    final accessToken = results[0];
    if (accessToken == null) return null;

    // Warm the in-memory cache on first access (e.g. after app restart)
    _cache.update(accessToken);

    return AuthToken(accessToken: accessToken, refreshToken: results[1]);
  }

  Future<void> clearToken() async {
    _cache.clear();
    await Future.wait([
      _storage.delete(key: AppConstants.accessTokenKey),
      _storage.delete(key: AppConstants.refreshTokenKey),
    ]);
  }
}
