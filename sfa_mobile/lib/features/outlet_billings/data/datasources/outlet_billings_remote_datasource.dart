import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/outlet_billings/data/models/outlet_summary_model.dart';
import 'package:uswatte/features/outlet_billings/data/models/route_assignment_model.dart';

class OutletBillingsRemoteDatasource {
  final Dio _dio;

  const OutletBillingsRemoteDatasource(this._dio);

  Future<List<RouteAssignmentModel>> getAssignedRoutes() async {
    try {
      final response = await _dio.get(
        '/api/v1/daily-route-assignments',
        queryParameters: {'pageSize': 200, 'page': 1},
      );
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as Map<String, dynamic>;
      final rawList = (data['assignments'] as List<dynamic>?) ?? [];
      return rawList
          .map((e) => RouteAssignmentModel.fromJson(e as Map<String, dynamic>))
          .toList();
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      final passthrough = e.error;
      if (passthrough is AppException) throw passthrough;
      final statusCode = e.response?.statusCode ?? 0;
      if (e.response?.data is Map<String, dynamic>) {
        final body = e.response!.data as Map<String, dynamic>;
        final errorJson = body['error'];
        if (errorJson is Map<String, dynamic>) {
          throw ApiError.fromJson(errorJson).toException(statusCode);
        }
      }
      throw NetworkException(message: _networkMessage(e));
    } catch (_) {
      throw const ParseException(
          message: 'Failed to load assigned routes from server.');
    }
  }

  Future<List<OutletSummaryModel>> getOutletSummary({
    required int routeId,
    required String dateFrom,
    required String dateTo,
  }) async {
    try {
      final response = await _dio.get(
        '/api/v1/billings/outlet-summary',
        queryParameters: {
          'routeId': routeId,
          'dateFrom': dateFrom,
          'dateTo': dateTo,
        },
      );
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as Map<String, dynamic>;
      final rawList = data['outletSummaries'] as List<dynamic>;
      return rawList
          .map((e) => OutletSummaryModel.fromJson(e as Map<String, dynamic>))
          .toList();
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      final passthrough = e.error;
      if (passthrough is AppException) throw passthrough;
      final statusCode = e.response?.statusCode ?? 0;
      if (e.response?.data is Map<String, dynamic>) {
        final body = e.response!.data as Map<String, dynamic>;
        final errorJson = body['error'];
        if (errorJson is Map<String, dynamic>) {
          throw ApiError.fromJson(errorJson).toException(statusCode);
        }
      }
      throw NetworkException(message: _networkMessage(e));
    } catch (_) {
      throw const ParseException(
          message: 'Failed to load outlet billing summary from server.');
    }
  }

  String _networkMessage(DioException e) => switch (e.type) {
        DioExceptionType.connectionTimeout ||
        DioExceptionType.sendTimeout =>
          'Connection timed out. Check your network.',
        DioExceptionType.receiveTimeout => 'Server took too long to respond.',
        DioExceptionType.connectionError => 'No internet connection.',
        DioExceptionType.cancel => 'Request was cancelled.',
        _ => 'Network error. Please try again.',
      };
}
