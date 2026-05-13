import 'package:dio/dio.dart';
import 'package:uswatte/features/item_wise_achievement/data/models/item_wise_achievement_model.dart';
import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';

class SupervisorAchievementRemoteDatasource {
  final Dio _dio;
  const SupervisorAchievementRemoteDatasource(this._dio);

  Future<ItemWiseAchievement> getRepAchievement(
      int userId, int year, int month) async {
    final response = await _dio.get(
      '/api/v1/supervisor/rep-achievement-itemwise',
      queryParameters: {'userId': userId, 'year': year, 'month': month},
    );
    final data = response.data['data'] as Map<String, dynamic>;
    return ItemWiseAchievementModel.fromJson(data);
  }

  Future<double> getRepMonthlySales(int userId, int year, int month) async {
    final response = await _dio.get(
      '/api/v1/supervisor/rep-monthly-sales',
      queryParameters: {'userId': userId, 'year': year, 'month': month},
    );
    final data = response.data['data'] as Map<String, dynamic>;
    return (data['totalSales'] as num?)?.toDouble() ?? 0.0;
  }

  Future<double> getRepMonthlyTarget(int userId, int year, int month) async {
    final response = await _dio.get(
      '/api/v1/supervisor/rep-monthly-target',
      queryParameters: {'userId': userId, 'year': year, 'month': month},
    );
    final data = response.data['data'] as Map<String, dynamic>;
    return (data['totalTarget'] as num?)?.toDouble() ?? 0.0;
  }
}
