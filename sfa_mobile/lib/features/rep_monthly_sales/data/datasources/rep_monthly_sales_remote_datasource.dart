import 'package:dio/dio.dart';
import 'package:uswatte/features/rep_monthly_sales/data/models/rep_daily_sales_model.dart';
import 'package:uswatte/features/rep_monthly_sales/data/models/rep_monthly_sales_model.dart';
import 'package:uswatte/features/rep_monthly_sales/domain/entities/rep_daily_sales.dart';
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

  Future<RepDailySales> getDailySales(DateTime date) async {
    final dateStr =
        '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
    final response = await _dio.get(
      '/api/v1/billings/my-daily-sales',
      queryParameters: {'date': dateStr},
    );
    final data = response.data['data'] as Map<String, dynamic>;
    return RepDailySalesModel.fromJson(data);
  }
}
