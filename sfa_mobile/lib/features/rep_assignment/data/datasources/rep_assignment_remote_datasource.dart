import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/rep_assignment/data/models/rep_assignment_model.dart';

class RepAssignmentRemoteDatasource {
  final Dio _dio;
  const RepAssignmentRemoteDatasource(this._dio);

  Future<RepAssignmentModel> getMyAssignment() async {
    try {
      final response = await _dio.get('/api/v1/user-assignments/me');
      final body = response.data as Map<String, dynamic>;
      return RepAssignmentModel.fromJson(body['data'] as Map<String, dynamic>);
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read assignment response.');
    }
  }

  AppException _mapDioError(DioException e) {
    final interceptorError = e.error;
    if (interceptorError is AppException) return interceptorError;

    final statusCode = e.response?.statusCode ?? 500;
    if (e.response?.data is Map<String, dynamic>) {
      final body = e.response!.data as Map<String, dynamic>;
      final errorJson = body['error'];
      if (errorJson is Map<String, dynamic>) {
        return ApiError.fromJson(errorJson).toException(statusCode);
      }
    }

    final message = switch (e.type) {
      DioExceptionType.connectionTimeout ||
      DioExceptionType.sendTimeout =>
        'Connection timed out. Check your network.',
      DioExceptionType.receiveTimeout => 'Server took too long to respond.',
      DioExceptionType.connectionError => 'No internet connection.',
      DioExceptionType.cancel => 'Request was cancelled.',
      _ => 'Network error. Please try again.',
    };
    return NetworkException(message: message);
  }
}
