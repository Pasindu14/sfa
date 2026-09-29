import 'package:dio/dio.dart';
import 'package:uswatte/features/supervisor_itemwise_sales/domain/entities/rep_itemwise_sales.dart';

class SupervisorItemwiseSalesRemoteDatasource {
  final Dio _dio;
  const SupervisorItemwiseSalesRemoteDatasource(this._dio);

  static String _ymd(DateTime d) =>
      '${d.year}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  Future<RepItemwiseSales> getRepItemwiseSales(
    int userId,
    DateTime from,
    DateTime to,
  ) async {
    final response = await _dio.get(
      '/api/v1/supervisor/rep-itemwise-sales',
      queryParameters: {'userId': userId, 'from': _ymd(from), 'to': _ymd(to)},
    );
    final data = response.data['data'] as Map<String, dynamic>;
    return RepItemwiseSales.fromJson(data);
  }
}
