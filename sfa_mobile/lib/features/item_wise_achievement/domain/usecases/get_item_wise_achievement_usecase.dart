import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';
import 'package:uswatte/features/item_wise_achievement/domain/repositories/item_wise_achievement_repository.dart';

class GetItemWiseAchievementUseCase {
  final ItemWiseAchievementRepository _repository;
  const GetItemWiseAchievementUseCase(this._repository);

  Future<ItemWiseAchievement> call(int year, int month) =>
      _repository.getItemWiseAchievement(year, month);
}
