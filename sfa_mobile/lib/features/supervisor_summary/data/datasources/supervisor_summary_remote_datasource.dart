import 'package:dio/dio.dart';
import 'package:uswatte/features/supervisor_summary/data/models/supervisor_summary_model.dart';
import 'package:uswatte/features/supervisor_summary/domain/entities/supervisor_summary.dart';

class SupervisorSummaryRemoteDatasource {
  final Dio _dio;
  const SupervisorSummaryRemoteDatasource(this._dio);

  Future<SupervisorSummary> getSummary(String date) async {
    final response = await _dio.get(
      '/api/v1/supervisor/summary',
      queryParameters: {'date': date},
    );
    final data = response.data['data'] as Map<String, dynamic>;
    return SupervisorSummaryModel.fromJson(data);
  }
}
