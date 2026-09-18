import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/bills/data/models/bill_item_model.dart';
import 'package:uswatte/features/bills/data/models/bill_model.dart';
import 'package:uswatte/features/bills/domain/entities/bill_item.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';

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

  /// Pulls the rep's own bills back down from the server.
  /// GET /api/v1/billings/my-bills/sync?days=N
  ///
  /// Bills created on this device carry a `clientBillId`, which is what lets the local store
  /// recognise a bill it already holds. Bills without one were written elsewhere (the web app),
  /// so they get a stable synthetic key derived from the server id — stable being the point:
  /// syncing repeatedly must not keep inserting the same bill under new keys.
  Future<List<BillModel>> fetchMyBillsForSync({int days = 7}) async {
    try {
      final response = await _dio.get(
        '/api/v1/billings/my-bills/sync',
        queryParameters: {'days': days},
      );
      // Read the envelope directly: ApiResponse.fromJson casts `data` to a Map, so it cannot
      // carry a list payload.
      final envelope = response.data as Map<String, dynamic>;
      final rows = (envelope['data'] as List<dynamic>?) ?? const <dynamic>[];
      return rows
          .map((row) => _billFromServer(row as Map<String, dynamic>))
          .toList();
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      final passthrough = e.error;
      if (passthrough is AppException) throw passthrough;
      throw NetworkException(message: _networkMessage(e));
    }
  }

  BillModel _billFromServer(Map<String, dynamic> json) {
    final serverBillId = json['id'] as int;
    final clientBillId =
        (json['clientBillId'] as String?) ?? 'srv-$serverBillId';

    final items = ((json['items'] as List<dynamic>?) ?? const <dynamic>[])
        .map((raw) {
      final item = raw as Map<String, dynamic>;
      final expire = item['expireDate'] as String?;
      return BillItemModel(
        clientBillId: clientBillId,
        productId: item['productId'] as int,
        quantity: (item['quantity'] as num).toDouble(),
        unitPrice: (item['unitPrice'] as num).toDouble(),
        discountRate: (item['discountRate'] as num?)?.toDouble() ?? 0,
        billingItemType: item['billingItemType'] as String? ?? 'Sale',
        returnType: item['returnType'] as String?,
        freeIssueSource: item['freeIssueSource'] as String?,
        expireDate: expire != null ? DateTime.parse(expire) : null,
        lineNumber: item['lineNumber'] as int,
        // The server records a basis, not a case/packet split; recover the
        // split so the detail page keeps case and packet lines apart.
        priceType: priceTypeForBasis(item['priceBasis'] as String?),
        pricingStructureId: item['pricingStructureId'] as int?,
        listUnitPrice: (item['listUnitPrice'] as num?)?.toDouble(),
      );
    }).toList();

    return BillModel(
      clientBillId: clientBillId,
      outletId: json['outletId'] as int,
      billingDate: DateTime.parse(json['billingDate'] as String),
      billDiscountRate: (json['billDiscountRate'] as num?)?.toDouble() ?? 0,
      subTotalAmount: (json['subTotalAmount'] as num).toDouble(),
      billDiscountAmount: (json['billDiscountAmount'] as num?)?.toDouble() ?? 0,
      totalAmount: (json['totalAmount'] as num).toDouble(),
      notes: json['notes'] as String?,
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      createdAt: DateTime.parse(json['createdAt'] as String),
      // It is on the server, so by definition it is synced.
      syncStatus: SyncStatus.synced,
      serverBillId: serverBillId,
      serverBillNumber: json['billingNumber'] as String?,
      outletName: json['outletName'] as String?,
      pricingStructureId: json['pricingStructureId'] as int?,
      items: items,
    );
  }

  /// Cancels a synced billing on the server.
  /// PATCH /api/v1/billings/{serverBillId}/cancel
  Future<void> cancelBilling(int serverBillId) async {
    try {
      await _dio.patch('/api/v1/billings/$serverBillId/cancel');
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      final passthrough = e.error;
      if (passthrough is AppException) throw passthrough;
      throw NetworkException(message: _networkMessage(e));
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
