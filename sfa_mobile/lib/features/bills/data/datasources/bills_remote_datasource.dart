import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/bills/data/models/bill_model.dart';

/// Result of a successful POST /api/v1/billings — only the fields we need
/// to update the local outbox row.
class CreateBillingResponse {
  final int serverBillId;
  final String serverBillNumber;

  const CreateBillingResponse({
    required this.serverBillId,
    required this.serverBillNumber,
  });
}

class BillsRemoteDatasource {
  final Dio _dio;

  const BillsRemoteDatasource(this._dio);

  /// Creates a billing on the server.
  ///
  /// [clientBillId] is sent as the `X-Idempotency-Key` header — the server
  /// caches the response keyed by `{userId}:{clientBillId}`, so retries on
  /// flaky networks never produce duplicate rows.
  ///
  /// On 422 `INSUFFICIENT_STOCK`, per-product shortage messages are joined
  /// into the exception's `detail` so the UI can render every blocked product
  /// on the Failed bill detail page.
  Future<CreateBillingResponse> createBilling(BillModel bill) async {
    try {
      final response = await _dio.post(
        '/api/v1/billings',
        data: bill.toCreateRequestJson(),
        options: Options(headers: {'X-Idempotency-Key': bill.clientBillId}),
      );
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as Map<String, dynamic>;
      return CreateBillingResponse(
        serverBillId: data['id'] as int,
        serverBillNumber: data['billingNumber'] as String,
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

          // Stock-out: flatten per-product messages into detail so the
          // Bill detail view can render one line per blocked product.
          if (apiError.code == 'INSUFFICIENT_STOCK' && apiError.fields.isNotEmpty) {
            final detail = apiError.fields.values
                .expand((msgs) => msgs)
                .join('\n');
            throw BusinessRuleException(
              code: apiError.code,
              message: apiError.message,
              detail: detail,
            );
          }

          throw apiError.toException(statusCode);
        }
      }

      // No parseable body — surface as network error. The outbox interprets
      // this as "retry when connection returns" rather than "failed".
      throw NetworkException(message: _networkMessage(e));
    } catch (_) {
      throw const ParseException(
        message: 'Failed to read create-billing response from server.',
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
