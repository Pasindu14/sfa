import 'package:uswatte/core/utils/bill_breakdown.dart';

class MyBillSummary {
  final int id;
  final String billingNumber;
  final DateTime billingDate;
  final int outletId;
  final String outletName;
  final String distributorName;
  final double totalAmount;

  /// Breakdown: Sales (gross) − Discount − Returns = [totalAmount]. Free issues are informational.
  final double grossAmount;
  final double totalDiscount;
  final double returnValue;
  final double freeIssueValue;
  final String repStatus;
  final String distributorStatus;
  final DateTime createdAt;

  const MyBillSummary({
    required this.id,
    required this.billingNumber,
    required this.billingDate,
    required this.outletId,
    required this.outletName,
    required this.distributorName,
    required this.totalAmount,
    required this.grossAmount,
    this.totalDiscount = 0,
    this.returnValue = 0,
    this.freeIssueValue = 0,
    required this.repStatus,
    required this.distributorStatus,
    required this.createdAt,
  });

  BillBreakdown get breakdown => BillBreakdown.forList(
        total: totalAmount,
        gross: grossAmount,
        discount: totalDiscount,
        returns: returnValue,
        freeIssue: freeIssueValue,
      );
}
