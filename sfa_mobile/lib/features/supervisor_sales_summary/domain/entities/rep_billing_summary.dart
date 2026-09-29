import 'package:equatable/equatable.dart';

/// One rep's billing totals over a date range
/// (`GET /api/v1/supervisor/rep-billing-summary`).
///
/// Only distributor-approved, non-cancelled bills are sales. Money figures
/// cover approved + pending bills; rejected/cancelled are counts only.
/// Good return = market resell; market return = damage + expire.
class RepBillingSummary extends Equatable {
  final DateTime from;
  final DateTime to;
  final int totalBills;
  final int approvedCount;
  final int pendingCount;
  final int rejectedCount;
  final int cancelledCount;
  final double totalBilled;
  final double approvedSales;
  final double pendingValue;
  final double totalDiscount;
  final double goodReturn;
  final double marketReturn;

  const RepBillingSummary({
    required this.from,
    required this.to,
    required this.totalBills,
    required this.approvedCount,
    required this.pendingCount,
    required this.rejectedCount,
    required this.cancelledCount,
    required this.totalBilled,
    required this.approvedSales,
    required this.pendingValue,
    required this.totalDiscount,
    required this.goodReturn,
    required this.marketReturn,
  });

  factory RepBillingSummary.fromJson(Map<String, dynamic> json) {
    double d(String k) => (json[k] as num?)?.toDouble() ?? 0.0;
    int i(String k) => (json[k] as num?)?.toInt() ?? 0;
    return RepBillingSummary(
      from: DateTime.parse(json['from'] as String),
      to: DateTime.parse(json['to'] as String),
      totalBills: i('totalBills'),
      approvedCount: i('approvedCount'),
      pendingCount: i('pendingCount'),
      rejectedCount: i('rejectedCount'),
      cancelledCount: i('cancelledCount'),
      totalBilled: d('totalBilled'),
      approvedSales: d('approvedSales'),
      pendingValue: d('pendingValue'),
      totalDiscount: d('totalDiscount'),
      goodReturn: d('goodReturn'),
      marketReturn: d('marketReturn'),
    );
  }

  @override
  List<Object?> get props => [
        from,
        to,
        totalBills,
        approvedCount,
        pendingCount,
        rejectedCount,
        cancelledCount,
        totalBilled,
        approvedSales,
        pendingValue,
        totalDiscount,
        goodReturn,
        marketReturn,
      ];
}
