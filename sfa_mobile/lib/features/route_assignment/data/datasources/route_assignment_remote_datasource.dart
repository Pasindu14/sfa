import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/route_assignment/data/models/daily_route_assignment_model.dart';
import 'package:uswatte/features/route_assignment/data/models/rep_route_model.dart';
import 'package:uswatte/features/route_assignment/data/models/rep_summary_model.dart';
import 'package:uswatte/features/route_assignment/domain/entities/daily_route_assignment.dart';

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

  Future<AssignmentsResult> getAssignments({
    int page = 1,
    int pageSize = 50,
    int? userId,
    DateTime? date,
  }) async {
    try {
      final queryParams = <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
        if (userId != null) 'userId': userId,
        if (date != null)
          'date':
              '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}',
      };
      final response = await _dio.get(
        '/api/v1/daily-route-assignments',
        queryParameters: queryParams,
      );
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as Map<String, dynamic>;
      final assignmentList = (data['assignments'] as List<dynamic>)
          .map((e) =>
              DailyRouteAssignmentModel.fromJson(e as Map<String, dynamic>))
          .toList();
      return AssignmentsResult(
        assignments: assignmentList,
        totalCount: data['totalCount'] as int,
        page: data['page'] as int,
        pageSize: data['pageSize'] as int,
      );
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read server response.');
    }
  }

  /// Returns the updated assignment if the supervisor flagged it as pending (HTTP 200),
  /// or null if it was directly deleted by an admin/manager (HTTP 204).
  Future<DailyRouteAssignment?> deleteAssignment(int id, {String? reason}) async {
    try {
      final response = await _dio.delete(
        '/api/v1/daily-route-assignments/$id',
        data: reason != null ? {'reason': reason} : null,
      );
      if (response.statusCode == 204) return null;
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as Map<String, dynamic>;
      return DailyRouteAssignmentModel.fromJson(data);
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
