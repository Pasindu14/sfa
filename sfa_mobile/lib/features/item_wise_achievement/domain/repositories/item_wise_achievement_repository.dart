import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';

abstract class ItemWiseAchievementRepository {
  Future<ItemWiseAchievement> getItemWiseAchievement(int year, int month);
}
