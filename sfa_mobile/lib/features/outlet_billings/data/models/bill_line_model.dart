import 'package:uswatte/features/outlet_billings/domain/entities/bill_line.dart';

class BillLineModel {
  final int id;
  final String billingNumber;
  final String billingDate;
  final double totalAmount;
  final double grossAmount;
  final double totalDiscount;
  final double returnValue;
  final double freeIssueValue;
  final String status;

  const BillLineModel({
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

  factory BillLineModel.fromJson(Map<String, dynamic> json) => BillLineModel(
        id: json['id'] as int,
        billingNumber: json['billingNumber'] as String,
        billingDate: json['billingDate'] as String,
        totalAmount: (json['totalAmount'] as num).toDouble(),
        // Newer fields: an older API build omits them, so fall back to a
        // plain bill (gross = total, no discount/returns) instead of crashing.
        grossAmount: ((json['grossAmount'] ?? json['totalAmount']) as num).toDouble(),
        totalDiscount: (json['totalDiscount'] as num?)?.toDouble() ?? 0,
        returnValue: (json['returnValue'] as num?)?.toDouble() ?? 0,
        freeIssueValue: (json['freeIssueValue'] as num?)?.toDouble() ?? 0,
        status: json['status'] as String,
      );

  BillLine toEntity() => BillLine(
        id: id,
        billingNumber: billingNumber,
        billingDate: billingDate,
        totalAmount: totalAmount,
        grossAmount: grossAmount,
        totalDiscount: totalDiscount,
        returnValue: returnValue,
        freeIssueValue: freeIssueValue,
        status: status,
      );
}
