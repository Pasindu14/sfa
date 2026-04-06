import 'dart:convert';

/// Decodes a JWT payload (without verifying the signature — verification
/// happens server-side). Used only to read claims like `role` from a
/// trusted token stored in secure storage.
class JwtDecoder {
  static const _roleClaimKey =
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

  /// Returns the role claim value, or null if the token is malformed.
  static String? extractRole(String token) {
    final parts = token.split('.');
    if (parts.length != 3) return null;
    // base64url payload may be unpadded — normalize adds '=' padding
    final normalized = base64Url.normalize(parts[1]);
    final decoded = utf8.decode(base64Url.decode(normalized));
    final claims = jsonDecode(decoded) as Map<String, dynamic>;
    return claims[_roleClaimKey] as String?;
  }
}
