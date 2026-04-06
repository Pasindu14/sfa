/// In-memory access token cache.
/// Prevents a disk read (secure storage) on every API request.
/// Updated by [AuthLocalDatasource] on save/clear.
class TokenCache {
  String? _accessToken;

  String? get accessToken => _accessToken;

  void update(String token) => _accessToken = token;
  void clear() => _accessToken = null;
}
