import 'package:dio/dio.dart';
import 'package:uswatte/features/supervisor_sales_summary/domain/entities/rep_billing_summary.dart';

/// The calling rep's own billing summary — same payload the supervisor sees
/// for a rep (`/supervisor/rep-billing-summary`), scoped server-side to the JWT user.
class RepBillingSummaryRemoteDatasource {
  final Dio _dio;
  const RepBillingSummaryRemoteDatasource(this._dio);

  static String _ymd(DateTime d) =>
      '${d.year}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  Future<RepBillingSummary> getMyBillingSummary(
      DateTime from, DateTime to) async {
    final response = await _dio.get(
      '/api/v1/billings/my-billing-summary',
      queryParameters: {'from': _ymd(from), 'to': _ymd(to)},
    );
    final data = response.data['data'] as Map<String, dynamic>;
    return RepBillingSummary.fromJson(data);
  }
}
