import 'package:uswatte/features/supervisor_summary/domain/entities/supervisor_summary.dart';

class SupervisorSummaryModel extends SupervisorSummary {
  const SupervisorSummaryModel({
    required super.totalReps,
    required super.assignedReps,
    required super.billsToday,
    required super.nonBillingsToday,
    required super.totalSalesToday,
  });

  factory SupervisorSummaryModel.fromJson(Map<String, dynamic> json) {
    return SupervisorSummaryModel(
      totalReps: json['totalReps'] as int? ?? 0,
      assignedReps: json['assignedReps'] as int? ?? 0,
      billsToday: json['billsToday'] as int? ?? 0,
      nonBillingsToday: json['nonBillingsToday'] as int? ?? 0,
      totalSalesToday: (json['totalSalesToday'] as num?)?.toDouble() ?? 0.0,
    );
  }
}
