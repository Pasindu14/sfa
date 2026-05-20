import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import '../models/purchase_order_detail_model.dart';
import '../models/purchase_order_summary_model.dart';

class PurchaseOrdersRemoteDatasource {
  final Dio _dio;
  const PurchaseOrdersRemoteDatasource(this._dio);

  // status=1 → PendingRepApproval
  Future<List<PurchaseOrderSummaryModel>> getPendingOrders({
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final response = await _dio.get(
        '/api/v1/purchase-orders',
        queryParameters: {
          'status': 'PendingRepApproval',
          'page': page,
          'pageSize': pageSize,
        },
      );
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as Map<String, dynamic>;
      final list = data['purchaseOrders'] as List<dynamic>;
      return list
          .map((e) => PurchaseOrderSummaryModel.fromJson(e as Map<String, dynamic>))
          .toList();
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read purchase orders response.');
    }
  }

  Future<PurchaseOrderDetailModel> getOrderById(int id) async {
    try {
      final response = await _dio.get('/api/v1/purchase-orders/$id');
      final body = response.data as Map<String, dynamic>;
      return PurchaseOrderDetailModel.fromJson(body['data'] as Map<String, dynamic>);
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    } catch (_) {
      throw const ParseException(message: 'Failed to read purchase order detail.');
    }
  }

  Future<void> repApprove(int id) async {
    try {
      await _dio.post('/api/v1/purchase-orders/$id/rep-approve');
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    }
  }

  Future<void> reject(int id, String reason) async {
    try {
      await _dio.post(
        '/api/v1/purchase-orders/$id/reject',
        data: {'reason': reason},
      );
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
