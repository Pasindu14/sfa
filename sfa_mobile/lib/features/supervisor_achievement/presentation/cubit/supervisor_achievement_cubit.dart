import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/supervisor_achievement/data/datasources/supervisor_achievement_remote_datasource.dart';
import 'supervisor_achievement_state.dart';

class SupervisorAchievementCubit extends Cubit<SupervisorAchievementState> {
  final GetMyRepsUseCase _getMyReps;
  final SupervisorAchievementRemoteDatasource _remote;

  SupervisorAchievementCubit({
    required GetMyRepsUseCase getMyReps,
    required SupervisorAchievementRemoteDatasource remote,
  })  : _getMyReps = getMyReps,
        _remote = remote,
        super(const SupervisorAchievementLoadingReps());

  Future<void> loadReps() async {
    emit(const SupervisorAchievementLoadingReps());
    try {
      final reps = await _getMyReps();
      final now = DateTime.now();
      emit(SupervisorAchievementReady(
        reps: reps,
        month: now.month,
        year: now.year,
      ));
    } catch (e) {
      emit(SupervisorAchievementRepsError(e.toString()));
    }
  }

  void selectRep(RepSummary rep) {
    final s = state;
    if (s is! SupervisorAchievementReady) return;
    emit(s.copyWith(
        selectedRep: rep, clearData: true, clearValues: true, clearError: true));
    _loadData();
  }

  void changeMonth(int year, int month) {
    final s = state;
    if (s is! SupervisorAchievementReady) return;
    emit(s.copyWith(
        year: year, month: month,
        clearData: true, clearValues: true, clearError: true));
    if (s.selectedRep != null) _loadData();
  }

  Future<void> refresh() async {
    final s = state;
    if (s is! SupervisorAchievementReady || s.selectedRep == null) return;
    await _loadData();
  }

  Future<void> _loadData() async {
    final s = state;
    if (s is! SupervisorAchievementReady || s.selectedRep == null) return;
    final rep = s.selectedRep!;
    final year = s.year;
    final month = s.month;
    emit(s.copyWith(isLoading: true, clearError: true));
    try {
      // Fetch item list, MTD sales, and monthly target in parallel.
      final results = await Future.wait([
        _remote.getRepAchievement(rep.userId, year, month),
        _remote.getRepMonthlySales(rep.userId, year, month),
        _remote.getRepMonthlyTarget(rep.userId, year, month),
      ]);
      final current = state;
      if (current is SupervisorAchievementReady) {
        emit(current.copyWith(
          isLoading: false,
          data: results[0] as ItemWiseAchievement,
          totalSales: results[1] as double,
          totalTarget: results[2] as double,
        ));
      }
    } catch (e) {
      final current = state;
      if (current is SupervisorAchievementReady) {
        emit(current.copyWith(
            isLoading: false,
            dataError: e.toString(),
            clearData: true,
            clearValues: true));
      }
    }
  }
}
