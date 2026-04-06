import 'dart:convert';

/// Decodes a JWT payload (without verifying the signature — verification
/// happens server-side). Used only to read claims like `role` from a
/// trusted token stored in secure storage.
class JwtDecoder {
  static const _roleClaimKey =
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
  static const _nameClaimKey = 'name';

  static Map<String, dynamic>? _decode(String token) {
    try {
      final parts = token.split('.');
      if (parts.length != 3) return null;
      final normalized = base64Url.normalize(parts[1]);
      final decoded = utf8.decode(base64Url.decode(normalized));
      return jsonDecode(decoded) as Map<String, dynamic>;
    } catch (_) {
      return null;
    }
  }

  /// Returns the role claim value, or null if the token is malformed.
  /// Never throws — any decode or cast failure returns null so the caller
  /// can apply a safe fallback without a try-catch at every call site.
  static String? extractRole(String token) {
    return _decode(token)?[_roleClaimKey] as String?;
  }

  /// Returns the name claim value, or null if the token is malformed.
  static String? extractName(String token) {
    return _decode(token)?[_nameClaimKey] as String?;
  }
}
