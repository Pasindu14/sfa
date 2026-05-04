import 'package:dio/dio.dart';
import 'package:uswatte/features/supervisor_billing/data/models/billing_detail_model.dart';
import 'package:uswatte/features/supervisor_billing/data/models/billing_summary_model.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_detail.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';

class SupervisorBillingRemoteDatasource {
  final Dio _dio;

  SupervisorBillingRemoteDatasource(this._dio);

  Future<List<BillingSummary>> getSupervisorBillings({
    required int salesRepId,
    required String date,
    int page = 1,
    int pageSize = 50,
  }) async {
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
    return data
        .map((e) => BillingSummaryModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<BillingDetail> getBillingDetail(int id) async {
    final response = await _dio.get('/api/v1/billings/$id');
    final body = response.data as Map<String, dynamic>;
    return BillingDetailModel.fromJson(body['data'] as Map<String, dynamic>);
  }
}
