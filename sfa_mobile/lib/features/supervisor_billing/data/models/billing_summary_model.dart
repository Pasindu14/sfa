import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';

class BillingSummaryModel extends BillingSummary {
  const BillingSummaryModel({
    required super.id,
    required super.billingNumber,
    required super.billingDate,
    required super.outletId,
    required super.outletName,
    required super.salesRepId,
    required super.salesRepName,
    required super.distributorId,
    required super.distributorName,
    required super.totalAmount,
    super.grossAmount,
    super.totalDiscount,
    super.returnValue,
    super.freeIssueValue,
    required super.status,
    required super.createdAt,
  });

  factory BillingSummaryModel.fromJson(Map<String, dynamic> json) {
    return BillingSummaryModel(
      id: json['id'] as int,
      billingNumber: json['billingNumber'] as String,
      billingDate: json['billingDate'] as String,
      outletId: json['outletId'] as int,
      outletName: json['outletName'] as String,
      salesRepId: json['salesRepId'] as int,
      salesRepName: json['salesRepName'] as String,
      distributorId: json['distributorId'] as int,
      distributorName: json['distributorName'] as String,
      totalAmount: (json['totalAmount'] as num).toDouble(),
      grossAmount: (json['grossAmount'] as num?)?.toDouble(),
      totalDiscount: (json['totalDiscount'] as num?)?.toDouble() ?? 0,
      returnValue: (json['returnValue'] as num?)?.toDouble() ?? 0,
      freeIssueValue: (json['freeIssueValue'] as num?)?.toDouble() ?? 0,
      status: BillingStatus.fromJson(json),
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }
}
