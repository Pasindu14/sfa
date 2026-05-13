import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';

sealed class SupervisorAchievementState {
  const SupervisorAchievementState();
}

class SupervisorAchievementLoadingReps extends SupervisorAchievementState {
  const SupervisorAchievementLoadingReps();
}

class SupervisorAchievementRepsError extends SupervisorAchievementState {
  final String message;
  const SupervisorAchievementRepsError(this.message);
}

class SupervisorAchievementReady extends SupervisorAchievementState {
  final List<RepSummary> reps;
  final RepSummary? selectedRep;
  final int month;
  final int year;
  final bool isLoading;
  final ItemWiseAchievement? data;
  final double? totalSales;
  final double? totalTarget;
  final String? dataError;

  const SupervisorAchievementReady({
    required this.reps,
    required this.month,
    required this.year,
    this.selectedRep,
    this.isLoading = false,
    this.data,
    this.totalSales,
    this.totalTarget,
    this.dataError,
  });

  SupervisorAchievementReady copyWith({
    RepSummary? selectedRep,
    bool clearRep = false,
    int? month,
    int? year,
    bool? isLoading,
    ItemWiseAchievement? data,
    bool clearData = false,
    double? totalSales,
    double? totalTarget,
    bool clearValues = false,
    String? dataError,
    bool clearError = false,
  }) {
    return SupervisorAchievementReady(
      reps: reps,
      selectedRep: clearRep ? null : (selectedRep ?? this.selectedRep),
      month: month ?? this.month,
      year: year ?? this.year,
      isLoading: isLoading ?? this.isLoading,
      data: clearData ? null : (data ?? this.data),
      totalSales: clearValues ? null : (totalSales ?? this.totalSales),
      totalTarget: clearValues ? null : (totalTarget ?? this.totalTarget),
      dataError: clearError ? null : (dataError ?? this.dataError),
    );
  }
}
