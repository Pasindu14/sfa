import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/item_wise_achievement/domain/usecases/get_item_wise_achievement_usecase.dart';
import 'package:uswatte/features/item_wise_achievement/presentation/cubit/item_wise_achievement_state.dart';

class ItemWiseAchievementCubit extends Cubit<ItemWiseAchievementState> {
  final GetItemWiseAchievementUseCase _getAchievement;

  ItemWiseAchievementCubit(this._getAchievement)
      : super(const ItemWiseAchievementInitial());

  Future<void> load(int year, int month) async {
    emit(const ItemWiseAchievementLoading());
    try {
      final data = await _getAchievement(year, month);
      emit(ItemWiseAchievementLoaded(data));
    } catch (e) {
      emit(ItemWiseAchievementErrorState(e.toString()));
    }
  }
}
