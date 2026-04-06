import 'package:uswatte/core/errors/app_exception.dart';

/// Matches the SFA API envelope:
/// { "success": true, "data": {...}, "pagination": null, "traceId": "..." }
class ApiResponse<T> {
  final bool success;
  final T? data;
  final String traceId;

  const ApiResponse({
    required this.success,
    required this.traceId,
    this.data,
  });

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic> json)? fromJsonT,
  ) {
    return ApiResponse(
      success: json['success'] as bool,
      traceId: json['traceId'] as String? ?? '',
      data: fromJsonT != null && json['data'] != null
          ? fromJsonT(json['data'] as Map<String, dynamic>)
          : null,
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Error envelope
// ─────────────────────────────────────────────────────────────────────────────

/// Matches the SFA API error envelope:
/// { "success": false, "error": { "code": "...", "message": "...", ... } }
class ApiErrorResponse {
  final bool success;
  final ApiError error;

  const ApiErrorResponse({required this.success, required this.error});

  factory ApiErrorResponse.fromJson(Map<String, dynamic> json) {
    return ApiErrorResponse(
      success: json['success'] as bool? ?? false,
      error: ApiError.fromJson(json['error'] as Map<String, dynamic>),
    );
  }
}

/// Parsed representation of `ApiError` from the server.
/// Call [toException] with the HTTP status code to get a typed [AppException].
class ApiError {
  final String code;
  final String message;
  final String? detail;

  /// Field-level validation errors: field name → list of messages.
  /// Populated on 400 VALIDATION_FAILED responses.
  final Map<String, List<String>> fields;

  /// Server's current version of the resource, present on 409
  /// CONCURRENCY_CONFLICT responses so the UI can show the latest data.
  final Object? currentData;

  final String? traceId;

  const ApiError({
    required this.code,
    required this.message,
    this.detail,
    this.fields = const {},
    this.currentData,
    this.traceId,
  });

  factory ApiError.fromJson(Map<String, dynamic> json) {
    final rawFields = json['fields'] as Map<String, dynamic>?;
    return ApiError(
      code: json['code'] as String? ?? 'SERVER_ERROR',
      message: json['message'] as String? ?? 'An error occurred.',
      detail: json['detail'] as String?,
      fields: rawFields != null
          ? rawFields.map(
              (k, v) => MapEntry(k, (v as List<dynamic>).cast<String>()),
            )
          : const {},
      currentData: json['currentData'],
      traceId: json['traceId'] as String?,
    );
  }

  /// Maps this error to the correct typed [AppException].
  ///
  /// [statusCode] drives the exception *type*; [code] drives the *subtype*
  /// within the same HTTP status (e.g. distinguishing the four 401 variants).
  AppException toException(int statusCode) {
    switch (statusCode) {
      case 400:
        return ValidationException(message: message, fields: fields);

      case 401:
        return switch (code) {
          'AUTH_INVALID_CREDENTIALS' => const InvalidCredentialsException(),
          'AUTH_ACCOUNT_DISABLED'    => const AccountDisabledException(),
          'AUTH_DEVICE_MISMATCH'     => const DeviceMismatchException(),
          'AUTH_TOKEN_EXPIRED'       => const UnauthorizedException(),
          'AUTH_INVALID_TOKEN'       => const InvalidTokenException(),
          _                          => AuthenticationException(code: code, message: message),
        };

      case 403:
        return AuthorizationException(message: message);

      case 404:
        return NotFoundException(code: code, message: message);

      case 409:
        return ConflictException(
          code: code,
          message: message,
          currentData: currentData,
        );

      case 422:
        return BusinessRuleException(
          code: code,
          message: message,
          detail: detail,
        );

      case 429:
        return const RateLimitException();

      case 503:
        return ServiceUnavailableException(message: message);

      default:
        // 500 and any unmapped status fall here
        return ServerException(code: code, message: message);
    }
  }
}
