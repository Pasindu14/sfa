import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/auth/data/models/login_request_model.dart';
import 'package:uswatte/features/auth/data/models/token_response_model.dart';

class AuthRemoteDatasource {
  final Dio _dio;

  const AuthRemoteDatasource(this._dio);

  Future<TokenResponseModel> login({
    required String username,
    required String password,
    required String deviceId,
  }) async {
    try {
      final response = await _dio.post(
        '/api/v1/auth/login',
        data: LoginRequestModel(
          username: username,
          password: password,
          deviceId: deviceId,
        ).toJson(),
      );

      final apiResponse = ApiResponse.fromJson(
        response.data as Map<String, dynamic>,
        TokenResponseModel.fromJson,
      );

      // Guard against success:true but null data (malformed API response)
      final data = apiResponse.data;
      if (data == null) {
        throw const ServerException(
          code: 'INVALID_RESPONSE',
          message: 'Server returned an empty response.',
        );
      }

      return data;
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      // Unwrap typed AppExceptions set by interceptors (e.g. UnauthorizedException
      // from TokenInterceptor when a protected endpoint returns 401).
      final interceptorError = e.error;
      if (interceptorError is AppException) throw interceptorError;

      // Parse the SFA ApiErrorResponse envelope: { "success": false, "error": {...} }
      // and convert to the correct typed AppException via the HTTP status code.
      final statusCode = e.response?.statusCode ?? 500;
      if (e.response?.data is Map<String, dynamic>) {
        final body = e.response!.data as Map<String, dynamic>;
        final errorJson = body['error'];
        if (errorJson is Map<String, dynamic>) {
          throw ApiError.fromJson(errorJson).toException(statusCode);
        }
      }

      // No parseable response body — map Dio error type to a network exception.
      final message = switch (e.type) {
        DioExceptionType.connectionTimeout || DioExceptionType.sendTimeout =>
          'Connection timed out. Check your network.',
        DioExceptionType.receiveTimeout => 'Server took too long to respond.',
        DioExceptionType.connectionError => 'No internet connection.',
        DioExceptionType.cancel => 'Request was cancelled.',
        _ => 'Network error. Please try again.',
      };
      throw NetworkException(message: message);
    } catch (_) {
      // Catches TypeError from the `response.data as Map` cast or fromJson
      // field type mismatches — neither of which surface as DioExceptions.
      throw const ParseException(message: 'Failed to read server response.');
    }
  }
}
