import 'package:uswatte/features/item_wise_achievement/data/datasources/item_wise_achievement_remote_datasource.dart';
import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';
import 'package:uswatte/features/item_wise_achievement/domain/repositories/item_wise_achievement_repository.dart';

class ItemWiseAchievementRepositoryImpl implements ItemWiseAchievementRepository {
  final ItemWiseAchievementRemoteDatasource _datasource;
  const ItemWiseAchievementRepositoryImpl(this._datasource);

  @override
  Future<ItemWiseAchievement> getItemWiseAchievement(int year, int month) =>
      _datasource.getItemWiseAchievement(year, month);
}
