import 'package:equatable/equatable.dart';
import 'package:uswatte/core/utils/bill_breakdown.dart';

class BillLine extends Equatable {
  final int id;
  final String billingNumber;
  final String billingDate;
  final double totalAmount;

  /// Breakdown: Sales (gross) − Discount − Returns = [totalAmount]. Free issues are informational.
  final double grossAmount;
  final double totalDiscount;
  final double returnValue;
  final double freeIssueValue;
  final String status;

  const BillLine({
    required this.id,
    required this.billingNumber,
    required this.billingDate,
    required this.totalAmount,
    required this.grossAmount,
    this.totalDiscount = 0,
    this.returnValue = 0,
    this.freeIssueValue = 0,
    required this.status,
  });

  BillBreakdown get breakdown => BillBreakdown.forList(
        total: totalAmount,
        gross: grossAmount,
        discount: totalDiscount,
        returns: returnValue,
        freeIssue: freeIssueValue,
      );

  @override
  List<Object?> get props => [
        id,
        billingNumber,
        billingDate,
        totalAmount,
        grossAmount,
        totalDiscount,
        returnValue,
        freeIssueValue,
        status,
      ];
}
