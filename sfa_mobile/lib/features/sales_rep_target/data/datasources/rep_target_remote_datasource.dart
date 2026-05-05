import 'package:dio/dio.dart';
import 'package:uswatte/features/sales_rep_target/data/models/rep_monthly_target_model.dart';
import 'package:uswatte/features/sales_rep_target/domain/entities/rep_monthly_target.dart';

class RepTargetRemoteDatasource {
  final Dio _dio;
  const RepTargetRemoteDatasource(this._dio);

  Future<RepMonthlyTarget> getMonthlyTarget(int year, int month) async {
    final response = await _dio.get(
      '/api/v1/sales-targets/my-monthly-target',
      queryParameters: {'year': year, 'month': month},
    );
    final data = response.data['data'] as Map<String, dynamic>;
    return RepMonthlyTargetModel.fromJson(data);
  }
}
