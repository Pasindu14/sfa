import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_reason.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_summary.dart';

class NotBillingSummaryModel extends NotBillingSummary {
  const NotBillingSummaryModel({
    required super.id,
    required super.outletId,
    required super.salesRepId,
    required super.notBillingNumber,
    required super.notBillingDate,
    required super.outletName,
    required super.salesRepName,
    required super.reason,
    required super.createdAt,
  });

  factory NotBillingSummaryModel.fromJson(Map<String, dynamic> json) {
    return NotBillingSummaryModel(
      id: json['id'] as int,
      outletId: json['outletId'] as int,
      salesRepId: json['salesRepId'] as int,
      notBillingNumber: json['notBillingNumber'] as String,
      notBillingDate: json['notBillingDate'] as String,
      outletName: json['outletName'] as String,
      salesRepName: json['salesRepName'] as String,
      reason: _parseReason(json['reason'] as String),
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }

  static NotBillingReason _parseReason(String s) {
    switch (s) {
      case 'OutletClosed':
        return NotBillingReason.outletClosed;
      case 'OwnerAbsent':
        return NotBillingReason.ownerAbsent;
      case 'CreditIssue':
        return NotBillingReason.creditIssue;
      case 'NoOrder':
        return NotBillingReason.noOrder;
      case 'OutOfStock':
        return NotBillingReason.outOfStock;
      default:
        return NotBillingReason.noOrder;
    }
  }
}
