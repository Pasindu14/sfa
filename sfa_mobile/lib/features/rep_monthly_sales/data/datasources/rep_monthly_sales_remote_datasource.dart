import 'package:dio/dio.dart';
import 'package:uswatte/features/rep_monthly_sales/data/models/rep_monthly_sales_model.dart';
import 'package:uswatte/features/rep_monthly_sales/domain/entities/rep_monthly_sales.dart';

class RepMonthlySalesRemoteDatasource {
  final Dio _dio;
  const RepMonthlySalesRemoteDatasource(this._dio);

  Future<RepMonthlySales> getMonthlySales(int year, int month) async {
    final response = await _dio.get(
      '/api/v1/billings/my-monthly-sales',
      queryParameters: {'year': year, 'month': month},
    );
    final data = response.data['data'] as Map<String, dynamic>;
    return RepMonthlySalesModel.fromJson(data);
  }
}
