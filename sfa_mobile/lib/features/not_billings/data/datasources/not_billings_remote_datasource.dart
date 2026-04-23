import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/not_billings/data/models/not_billing_model.dart';

class CreateNotBillingResponse {
  final int serverNotBillingId;
  final String serverNotBillingNumber;

  const CreateNotBillingResponse({
    required this.serverNotBillingId,
    required this.serverNotBillingNumber,
  });
}

class NotBillingsRemoteDatasource {
  final Dio _dio;

  const NotBillingsRemoteDatasource(this._dio);

  /// Creates a not-billing record on the server.
  ///
  /// [clientNotBillingId] is sent as the `X-Idempotency-Key` header so retries
  /// on flaky connections never produce duplicate records.
  Future<CreateNotBillingResponse> createNotBilling(NotBillingModel record) async {
    try {
      final response = await _dio.post(
        '/api/v1/not-billings',
        data: record.toCreateRequestJson(),
        options: Options(headers: {'X-Idempotency-Key': record.clientNotBillingId}),
      );
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as Map<String, dynamic>;
      return CreateNotBillingResponse(
        serverNotBillingId: data['id'] as int,
        serverNotBillingNumber: data['notBillingNumber'] as String,
      );
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      final passthrough = e.error;
      if (passthrough is AppException) throw passthrough;

      final statusCode = e.response?.statusCode ?? 0;
      final data = e.response?.data;
      if (data is Map<String, dynamic>) {
        final errorJson = data['error'];
        if (errorJson is Map<String, dynamic>) {
          final apiError = ApiError.fromJson(errorJson);
          throw apiError.toException(statusCode);
        }
      }

      throw NetworkException(message: _networkMessage(e));
    } catch (_) {
      throw const ParseException(
        message: 'Failed to read create-not-billing response from server.',
      );
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
