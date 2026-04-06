/// Base for all application-level exceptions.
/// [code] mirrors the API's `error.code` or a client-side sentinel.
abstract class AppException implements Exception {
  final String code;
  final String message;

  const AppException({required this.code, required this.message});

  @override
  String toString() => 'AppException($code): $message';
}

// ── Client-side ──────────────────────────────────────────────────────────────

/// No internet connection or connection/receive timeout.
class NetworkException extends AppException {
  const NetworkException({required super.message})
      : super(code: 'NETWORK_ERROR');
}

/// Response body could not be decoded into the expected model shape.
class ParseException extends AppException {
  const ParseException({required super.message})
      : super(code: 'PARSE_ERROR');
}

// ── 400 — Validation ─────────────────────────────────────────────────────────

/// One or more request fields failed server-side validation.
/// [fields] mirrors `error.fields`: field name → list of error messages.
class ValidationException extends AppException {
  final Map<String, List<String>> fields;

  const ValidationException({
    required super.message,
    this.fields = const {},
  }) : super(code: 'VALIDATION_FAILED');
}

// ── 401 — Authentication ─────────────────────────────────────────────────────

/// Base for all HTTP 401 authentication failures.
/// Use a specific subclass where possible for targeted UI handling.
class AuthenticationException extends AppException {
  const AuthenticationException({required super.code, required super.message});
}

/// Wrong username or password on login.
/// API code: AUTH_INVALID_CREDENTIALS
class InvalidCredentialsException extends AuthenticationException {
  const InvalidCredentialsException()
      : super(
          code: 'AUTH_INVALID_CREDENTIALS',
          message: 'Invalid username or password.',
        );
}

/// Account has been deactivated by an administrator.
/// API code: AUTH_ACCOUNT_DISABLED
class AccountDisabledException extends AuthenticationException {
  const AccountDisabledException()
      : super(
          code: 'AUTH_ACCOUNT_DISABLED',
          message:
              'Your account has been disabled. Contact your administrator.',
        );
}

/// Login attempt from a device not bound to this account.
/// API code: AUTH_DEVICE_MISMATCH
class DeviceMismatchException extends AuthenticationException {
  const DeviceMismatchException()
      : super(
          code: 'AUTH_DEVICE_MISMATCH',
          message:
              'This device is not registered for your account. Contact your administrator.',
        );
}

/// Access token has expired — trigger a refresh or re-login.
/// Thrown by [TokenInterceptor] on 401 for protected endpoints.
/// API code: AUTH_TOKEN_EXPIRED
class UnauthorizedException extends AuthenticationException {
  const UnauthorizedException()
      : super(
          code: 'AUTH_TOKEN_EXPIRED',
          message: 'Session expired. Please log in again.',
        );
}

/// Token is invalid, consumed (reuse detected), or revoked server-side.
/// API code: AUTH_INVALID_TOKEN
class InvalidTokenException extends AuthenticationException {
  const InvalidTokenException()
      : super(
          code: 'AUTH_INVALID_TOKEN',
          message: 'Your session is no longer valid. Please log in again.',
        );
}

// ── 403 — Authorization ──────────────────────────────────────────────────────

/// Authenticated but not permitted to access the resource.
/// API code: FORBIDDEN_ACCESS
class AuthorizationException extends AppException {
  const AuthorizationException({required super.message})
      : super(code: 'FORBIDDEN_ACCESS');
}

// ── 404 — Not Found ──────────────────────────────────────────────────────────

/// The requested resource does not exist.
/// API code pattern: {ENTITY}_NOT_FOUND (e.g. USER_NOT_FOUND)
class NotFoundException extends AppException {
  const NotFoundException({required super.code, required super.message});
}

// ── 409 — Conflict ───────────────────────────────────────────────────────────

/// A write operation conflicts with existing data.
/// [currentData] carries the server's current version for concurrency conflicts
/// so the UI can show the latest state before asking the user to retry.
/// API codes: CONCURRENCY_CONFLICT, {ENTITY}_DUPLICATE
class ConflictException extends AppException {
  final Object? currentData;

  const ConflictException({
    required super.code,
    required super.message,
    this.currentData,
  });
}

// ── 422 — Business Rule ──────────────────────────────────────────────────────

/// The request was structurally valid but rejected by a domain rule.
/// [detail] is the server-supplied hint for why the rule was violated.
/// API codes: INSUFFICIENT_STOCK, INVALID_ORDER_STATE,
///            LEAD_ALREADY_CONVERTED, and any future 422 codes.
class BusinessRuleException extends AppException {
  final String? detail;

  const BusinessRuleException({
    required super.code,
    required super.message,
    this.detail,
  });
}

// ── 429 — Rate Limited ───────────────────────────────────────────────────────

/// Too many requests — back off and retry.
/// API code: RATE_LIMITED
class RateLimitException extends AppException {
  const RateLimitException()
      : super(
          code: 'RATE_LIMITED',
          message: 'Too many requests. Please wait a moment and try again.',
        );
}

// ── 500 — Internal Server Error ──────────────────────────────────────────────

/// An unexpected error occurred on the server.
/// Also used as the default for any unmapped error code.
/// API code: INTERNAL_ERROR (and any unrecognised code)
class ServerException extends AppException {
  const ServerException({required super.code, required super.message});
}

// ── 503 — Service Unavailable ────────────────────────────────────────────────

/// A backend dependency (DB, cache, lock service) is temporarily down.
/// API codes: SERVICE_UNAVAILABLE, LOCK_SERVICE_UNAVAILABLE
class ServiceUnavailableException extends AppException {
  const ServiceUnavailableException({required super.message})
      : super(code: 'SERVICE_UNAVAILABLE');
}
