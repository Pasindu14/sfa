import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_summary.dart';

class OutletBillSummaryModel {
  final int id;
  final String billingNumber;
  final String billingDate;
  final int outletId;
  final String outletName;
  final String salesRepName;
  final String distributorName;
  final double totalAmount;
  final double grossAmount;
  final double totalDiscount;
  final double returnValue;
  final double freeIssueValue;
  final String status;
  final String createdAt;

  const OutletBillSummaryModel({
    required this.id,
    required this.billingNumber,
    required this.billingDate,
    required this.outletId,
    required this.outletName,
    required this.salesRepName,
    required this.distributorName,
    required this.totalAmount,
    required this.grossAmount,
    this.totalDiscount = 0,
    this.returnValue = 0,
    this.freeIssueValue = 0,
    required this.status,
    required this.createdAt,
  });

  factory OutletBillSummaryModel.fromJson(Map<String, dynamic> json) =>
      OutletBillSummaryModel(
        id: json['id'] as int,
        billingNumber: json['billingNumber'] as String,
        billingDate: json['billingDate'] as String,
        outletId: json['outletId'] as int,
        outletName: json['outletName'] as String,
        salesRepName: json['salesRepName'] as String,
        distributorName: json['distributorName'] as String,
        totalAmount: (json['totalAmount'] as num).toDouble(),
        // Newer fields: an older API build omits them, so fall back to a
        // plain bill (gross = total, no discount/returns) instead of crashing.
        grossAmount: ((json['grossAmount'] ?? json['totalAmount']) as num).toDouble(),
        totalDiscount: (json['totalDiscount'] as num?)?.toDouble() ?? 0,
        returnValue: (json['returnValue'] as num?)?.toDouble() ?? 0,
        freeIssueValue: (json['freeIssueValue'] as num?)?.toDouble() ?? 0,
        status: json['repStatus'] as String,
        createdAt: json['createdAt'] as String,
      );

  OutletBillSummary toEntity() => OutletBillSummary(
        id: id,
        billingNumber: billingNumber,
        billingDate: DateTime.parse(billingDate),
        outletId: outletId,
        outletName: outletName,
        salesRepName: salesRepName,
        distributorName: distributorName,
        totalAmount: totalAmount,
        grossAmount: grossAmount,
        totalDiscount: totalDiscount,
        returnValue: returnValue,
        freeIssueValue: freeIssueValue,
        status: status,
        createdAt: DateTime.parse(createdAt),
      );
}
