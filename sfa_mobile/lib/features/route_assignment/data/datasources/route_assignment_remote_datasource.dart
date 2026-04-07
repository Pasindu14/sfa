import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/route_assignment/data/models/rep_route_model.dart';
import 'package:uswatte/features/route_assignment/data/models/rep_summary_model.dart';

class RouteAssignmentRemoteDatasource {
  final Dio _dio;
  const RouteAssignmentRemoteDatasource(this._dio);

  Future<List<RepSummaryModel>> getMyReps() async {
    try {
      final response =
          await _dio.get('/api/v1/daily-route-assignments/my-reps');
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as List<dynamic>;
      return data
          .map((e) => RepSummaryModel.fromJson(e as Map<String, dynamic>))
          .toList();
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read server response.');
    }
  }

  Future<List<RepRouteModel>> getRepRoutes(int userId) async {
    try {
      final response = await _dio
          .get('/api/v1/daily-route-assignments/rep-routes/$userId');
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as List<dynamic>;
      return data
          .map((e) => RepRouteModel.fromJson(e as Map<String, dynamic>))
          .toList();
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read server response.');
    }
  }

  Future<void> createAssignment({
    required int userId,
    required int routeId,
    required DateTime assignedDate,
  }) async {
    try {
      final dateStr =
          '${assignedDate.year.toString().padLeft(4, '0')}-${assignedDate.month.toString().padLeft(2, '0')}-${assignedDate.day.toString().padLeft(2, '0')}';
      await _dio.post('/api/v1/daily-route-assignments', data: {
        'userId': userId,
        'routeId': routeId,
        'assignedDate': dateStr,
      });
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read server response.');
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
