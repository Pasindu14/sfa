import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_reason.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/rep_not_billing_detail.dart';

class RepNotBillingDetailModel extends RepNotBillingDetail {
  const RepNotBillingDetailModel({
    required super.id,
    required super.outletId,
    required super.salesRepId,
    required super.notBillingNumber,
    required super.notBillingDate,
    required super.outletName,
    required super.salesRepName,
    super.supervisorName,
    required super.reason,
    super.notes,
    required super.createdAt,
  });

  factory RepNotBillingDetailModel.fromJson(Map<String, dynamic> json) {
    return RepNotBillingDetailModel(
      id: json['id'] as int,
      outletId: json['outletId'] as int,
      salesRepId: json['salesRepId'] as int,
      notBillingNumber: json['notBillingNumber'] as String,
      notBillingDate: json['notBillingDate'] as String,
      outletName: json['outletName'] as String,
      salesRepName: json['salesRepName'] as String,
      supervisorName: json['supervisorName'] as String?,
      reason: _parseReason(json['reason'] as String),
      notes: json['notes'] as String?,
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
