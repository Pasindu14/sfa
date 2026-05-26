import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import '../models/notification_model.dart';

class NotificationsRemoteDatasource {
  final Dio _dio;
  const NotificationsRemoteDatasource(this._dio);

  Future<(List<NotificationModel>, int)> getNotifications({
    required int page,
    required int pageSize,
  }) async {
    try {
      final response = await _dio.get(
        '/api/v1/notifications',
        queryParameters: {'page': page, 'pageSize': pageSize},
      );
      final data = (response.data as Map<String, dynamic>)['data'] as Map<String, dynamic>;
      final list = (data['notifications'] as List<dynamic>)
          .map((e) => NotificationModel.fromJson(e as Map<String, dynamic>))
          .toList();
      final totalCount = data['totalCount'] as int;
      return (list, totalCount);
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read notifications response.');
    }
  }

  Future<int> getUnreadCount() async {
    try {
      final response = await _dio.get('/api/v1/notifications/unread-count');
      final data = (response.data as Map<String, dynamic>)['data'] as Map<String, dynamic>;
      return data['count'] as int;
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read unread count.');
    }
  }

  Future<void> markRead(int id) async {
    try {
      await _dio.post('/api/v1/notifications/$id/read');
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    }
  }

  Future<void> markAllRead() async {
    try {
      await _dio.post('/api/v1/notifications/read-all');
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
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
