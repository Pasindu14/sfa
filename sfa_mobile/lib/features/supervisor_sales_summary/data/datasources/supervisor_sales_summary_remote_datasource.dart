import 'package:dio/dio.dart';
import 'package:uswatte/features/supervisor_sales_summary/domain/entities/rep_billing_summary.dart';

class SupervisorSalesSummaryRemoteDatasource {
  final Dio _dio;
  const SupervisorSalesSummaryRemoteDatasource(this._dio);

  static String _ymd(DateTime d) =>
      '${d.year}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  Future<RepBillingSummary> getRepBillingSummary(
      int userId, DateTime from, DateTime to) async {
    final response = await _dio.get(
      '/api/v1/supervisor/rep-billing-summary',
      queryParameters: {'userId': userId, 'from': _ymd(from), 'to': _ymd(to)},
    );
    final data = response.data['data'] as Map<String, dynamic>;
    return RepBillingSummary.fromJson(data);
  }
}
