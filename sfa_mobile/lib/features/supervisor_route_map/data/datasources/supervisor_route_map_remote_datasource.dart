import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/outlets/data/models/outlet_model.dart';

class SupervisorRouteMapRemoteDatasource {
  final Dio _dio;
  const SupervisorRouteMapRemoteDatasource(this._dio);

  Future<int?> getRepTodayRouteId(int userId, String date) async {
    try {
      final response = await _dio.get(
        '/api/v1/daily-route-assignments',
        queryParameters: {'userId': userId, 'date': date, 'page': 1, 'pageSize': 1},
      );
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as Map<String, dynamic>;
      final assignments = (data['assignments'] as List<dynamic>?) ?? [];
      if (assignments.isEmpty) return null;
      return (assignments.first as Map<String, dynamic>)['routeId'] as int?;
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read assignment data.');
    }
  }

  Future<List<OutletModel>> getOutletsByRoute(int routeId) async {
    try {
      final response = await _dio.get('/api/v1/outlets/by-route/$routeId');
      final body = response.data as Map<String, dynamic>;
      final rawList = body['data'] as List<dynamic>;
      return rawList
          .map((e) => OutletModel.fromJson(e as Map<String, dynamic>))
          .toList();
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read outlet data.');
    }
  }

  Future<Set<int>> getRepBilledOutletIds(int salesRepId, String date) async {
    try {
      final response = await _dio.get(
        '/api/v1/billings',
        queryParameters: {
          'salesRepId': salesRepId,
          'dateFrom': date,
          'dateTo': date,
          'pageSize': 500,
        },
      );
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as List<dynamic>;
      return data
          .map((e) => (e as Map<String, dynamic>)['outletId'] as int)
          .toSet();
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read billing data.');
    }
  }

  Future<Set<int>> getRepNotBilledOutletIds(int salesRepId, String date) async {
    try {
      final response = await _dio.get(
        '/api/v1/not-billings',
        queryParameters: {
          'salesRepId': salesRepId,
          'dateFrom': date,
          'dateTo': date,
          'pageSize': 500,
        },
      );
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as List<dynamic>;
      return data
          .map((e) => (e as Map<String, dynamic>)['outletId'] as int)
          .toSet();
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read not-billing data.');
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
