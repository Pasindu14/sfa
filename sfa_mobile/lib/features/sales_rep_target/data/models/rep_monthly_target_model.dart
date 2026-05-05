import 'package:uswatte/features/sales_rep_target/domain/entities/rep_monthly_target.dart';

class RepMonthlyTargetModel extends RepMonthlyTarget {
  const RepMonthlyTargetModel({
    required super.year,
    required super.month,
    required super.totalTarget,
  });

  factory RepMonthlyTargetModel.fromJson(Map<String, dynamic> json) {
    return RepMonthlyTargetModel(
      year:        json['year'] as int? ?? 0,
      month:       json['month'] as int? ?? 0,
      totalTarget: (json['totalTarget'] as num?)?.toDouble() ?? 0.0,
    );
  }
}
