import 'package:uswatte/features/rep_monthly_sales/domain/entities/rep_monthly_sales.dart';

class RepMonthlySalesModel extends RepMonthlySales {
  const RepMonthlySalesModel({
    required super.year,
    required super.month,
    required super.totalSales,
    super.pendingTotal,
  });

  factory RepMonthlySalesModel.fromJson(Map<String, dynamic> json) {
    return RepMonthlySalesModel(
      year:         json['year'] as int? ?? 0,
      month:        json['month'] as int? ?? 0,
      totalSales:   (json['totalSales'] as num?)?.toDouble() ?? 0.0,
      pendingTotal: (json['pendingTotal'] as num?)?.toDouble() ?? 0.0,
    );
  }
}
