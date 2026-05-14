import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/outlet_bill_history/data/models/outlet_bill_detail_model.dart';
import 'package:uswatte/features/outlet_bill_history/data/models/outlet_bill_summary_model.dart';

class OutletBillHistoryRemoteDatasource {
  final Dio _dio;

  const OutletBillHistoryRemoteDatasource(this._dio);

  Future<({List<OutletBillSummaryModel> bills, bool hasMore})> getBillsForOutlet({
    required int outletId,
    required String dateFrom,
    required String dateTo,
    required int page,
    int pageSize = 20,
  }) async {
    try {
      final response = await _dio.get(
        '/api/v1/billings',
        queryParameters: {
          'outletId': outletId,
          'dateFrom': dateFrom,
          'dateTo': dateTo,
          'page': page,
          'pageSize': pageSize,
        },
      );
      final body = response.data as Map<String, dynamic>;
      final rawList = body['data'] as List<dynamic>;
      final bills = rawList
          .map((e) => OutletBillSummaryModel.fromJson(e as Map<String, dynamic>))
          .toList();

      final pagination = body['pagination'] as Map<String, dynamic>?;
      final totalPages = (pagination?['totalPages'] as num?)?.toInt() ?? 1;
      final hasMore = page < totalPages;

      return (bills: bills, hasMore: hasMore);
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
          message: 'Failed to read outlet bill history from server.');
    }
  }

  Future<OutletBillDetailModel> getBillDetail(int billingId) async {
    try {
      final response = await _dio.get('/api/v1/billings/$billingId');
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as Map<String, dynamic>;
      return OutletBillDetailModel.fromJson(data);
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
          message: 'Failed to read bill detail from server.');
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
