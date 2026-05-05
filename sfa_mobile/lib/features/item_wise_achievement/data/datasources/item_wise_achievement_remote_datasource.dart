import 'package:dio/dio.dart';
import 'package:uswatte/features/item_wise_achievement/data/models/item_wise_achievement_model.dart';
import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';

class ItemWiseAchievementRemoteDatasource {
  final Dio _dio;
  const ItemWiseAchievementRemoteDatasource(this._dio);

  Future<ItemWiseAchievement> getItemWiseAchievement(int year, int month) async {
    final response = await _dio.get(
      '/api/v1/billings/my-monthly-sales-itemwise',
      queryParameters: {'year': year, 'month': month},
    );
    final data = response.data['data'] as Map<String, dynamic>;
    return ItemWiseAchievementModel.fromJson(data);
  }
}
