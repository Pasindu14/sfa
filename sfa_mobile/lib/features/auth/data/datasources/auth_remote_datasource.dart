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
  }) async {
    try {
      final response = await _dio.post(
        '/api/v1/auth/login',
        data: LoginRequestModel(
          username: username,
          password: password,
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
      if (e.response?.statusCode == 401) {
        throw const ServerException(
          code: 'INVALID_CREDENTIALS',
          message: 'Invalid username or password.',
        );
      }

      // Try to parse the SFA ApiError envelope from the response body
      if (e.response?.data is Map<String, dynamic>) {
        final body = e.response!.data as Map<String, dynamic>;
        throw ServerException(
          code: body['code'] as String? ?? 'SERVER_ERROR',
          message: body['message'] as String? ?? 'An error occurred.',
        );
      }

      // Map Dio exception types to human-readable messages
      final message = switch (e.type) {
        DioExceptionType.connectionTimeout || DioExceptionType.sendTimeout =>
          'Connection timed out. Check your network.',
        DioExceptionType.receiveTimeout => 'Server took too long to respond.',
        DioExceptionType.connectionError => 'No internet connection.',
        DioExceptionType.cancel => 'Request was cancelled.',
        _ => 'Network error. Please try again.',
      };
      throw NetworkException(message: message);
    }
  }
}
