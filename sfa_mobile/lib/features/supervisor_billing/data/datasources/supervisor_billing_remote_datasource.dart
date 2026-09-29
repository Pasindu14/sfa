import 'package:dio/dio.dart';
import 'package:uswatte/features/supervisor_billing/data/models/billing_detail_model.dart';
import 'package:uswatte/features/supervisor_billing/data/models/billing_summary_model.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_detail.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';

class SupervisorBillingRemoteDatasource {
  final Dio _dio;

  SupervisorBillingRemoteDatasource(this._dio);

  /// Upper bound on pages walked, so a misbehaving `totalPages` can't loop forever.
  static const _maxPages = 20;

  /// Returns every bill for the rep on [date], walking all pages — the page's
  /// sales/pending totals are summed client-side, so a truncated list would
  /// silently under-report.
  Future<List<BillingSummary>> getSupervisorBillings({
    required int salesRepId,
    required String date,
    int pageSize = 50,
  }) async {
    final all = <BillingSummary>[];
    var page = 1;
    while (true) {
      final response = await _dio.get(
        '/api/v1/billings',
        queryParameters: {
          'salesRepId': salesRepId,
          'dateFrom': date,
          'dateTo': date,
          'page': page,
          'pageSize': pageSize,
        },
      );
      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as List<dynamic>;
      all.addAll(data.map(
          (e) => BillingSummaryModel.fromJson(e as Map<String, dynamic>)));

      final totalPages =
          (body['pagination'] as Map<String, dynamic>?)?['totalPages'] as int?;
      if (data.isEmpty ||
          totalPages == null ||
          page >= totalPages ||
          page >= _maxPages) {
        return all;
      }
      page++;
    }
  }

  Future<BillingDetail> getBillingDetail(int id) async {
    final response = await _dio.get('/api/v1/billings/$id');
    final body = response.data as Map<String, dynamic>;
    return BillingDetailModel.fromJson(body['data'] as Map<String, dynamic>);
  }
}
