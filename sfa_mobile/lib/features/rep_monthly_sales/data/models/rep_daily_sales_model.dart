import 'package:uswatte/features/rep_monthly_sales/domain/entities/rep_daily_sales.dart';

class RepDailySalesModel extends RepDailySales {
  const RepDailySalesModel({
    required super.date,
    required super.approvedTotal,
    required super.pendingTotal,
  });

  factory RepDailySalesModel.fromJson(Map<String, dynamic> json) {
    return RepDailySalesModel(
      date:          DateTime.now(),
      approvedTotal: (json['approvedTotal'] as num?)?.toDouble() ?? 0.0,
      pendingTotal:  (json['pendingTotal'] as num?)?.toDouble() ?? 0.0,
    );
  }
}
