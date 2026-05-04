import 'package:dio/dio.dart';
import 'package:uswatte/features/supervisor_not_billing/data/models/not_billing_summary_model.dart';
import 'package:uswatte/features/supervisor_not_billing/data/models/rep_not_billing_detail_model.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_summary.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/rep_not_billing_detail.dart';

class SupervisorNotBillingRemoteDatasource {
  final Dio _dio;
  SupervisorNotBillingRemoteDatasource(this._dio);

  Future<List<NotBillingSummary>> getSupervisorNotBillings({
    required int salesRepId,
    required String date,
    int page = 1,
    int pageSize = 50,
  }) async {
    final response = await _dio.get(
      '/api/v1/not-billings',
      queryParameters: {
        'salesRepId': salesRepId,
        'dateFrom': date,
        'dateTo': date,
        'page': page,
        'pageSize': pageSize,
      },
    );
    final data = response.data['data'] as List<dynamic>;
    return data
        .map((e) =>
            NotBillingSummaryModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<RepNotBillingDetail> getNotBillingDetail(int id) async {
    final response = await _dio.get('/api/v1/not-billings/$id');
    return RepNotBillingDetailModel.fromJson(
        response.data['data'] as Map<String, dynamic>);
  }
}
