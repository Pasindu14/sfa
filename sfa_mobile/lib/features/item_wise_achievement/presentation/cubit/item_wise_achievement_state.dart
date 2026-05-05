import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';

sealed class ItemWiseAchievementState {
  const ItemWiseAchievementState();
}

class ItemWiseAchievementInitial extends ItemWiseAchievementState {
  const ItemWiseAchievementInitial();
}

class ItemWiseAchievementLoading extends ItemWiseAchievementState {
  const ItemWiseAchievementLoading();
}

class ItemWiseAchievementLoaded extends ItemWiseAchievementState {
  final ItemWiseAchievement data;
  const ItemWiseAchievementLoaded(this.data);
}

class ItemWiseAchievementErrorState extends ItemWiseAchievementState {
  final String message;
  const ItemWiseAchievementErrorState(this.message);
}
