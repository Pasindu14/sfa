import 'package:uswatte/features/supervisor_billing/data/models/billing_item_model.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_detail.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';

class BillingDetailModel extends BillingDetail {
  const BillingDetailModel({
    required super.id,
    required super.billingNumber,
    required super.billingDate,
    required super.outletId,
    required super.outletName,
    required super.salesRepId,
    required super.salesRepName,
    required super.distributorId,
    required super.distributorName,
    super.supervisorName,
    required super.subTotalAmount,
    required super.billDiscountRate,
    required super.billDiscountAmount,
    required super.totalAmount,
    required super.status,
    super.notes,
    required super.createdAt,
    required super.items,
  });

  static BillingStatus _parseStatus(String s) {
    switch (s.toLowerCase()) {
      case 'approved':
        return BillingStatus.approved;
      case 'cancelled':
        return BillingStatus.cancelled;
      default:
        return BillingStatus.submitted;
    }
  }

  factory BillingDetailModel.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'] as List<dynamic>? ?? [];
    return BillingDetailModel(
      id: json['id'] as int,
      billingNumber: json['billingNumber'] as String,
      billingDate: json['billingDate'] as String,
      outletId: json['outletId'] as int,
      outletName: json['outletName'] as String,
      salesRepId: json['salesRepId'] as int,
      salesRepName: json['salesRepName'] as String,
      distributorId: json['distributorId'] as int,
      distributorName: json['distributorName'] as String,
      supervisorName: json['supervisorName'] as String?,
      subTotalAmount: (json['subTotalAmount'] as num).toDouble(),
      billDiscountRate: (json['billDiscountRate'] as num).toDouble(),
      billDiscountAmount: (json['billDiscountAmount'] as num).toDouble(),
      totalAmount: (json['totalAmount'] as num).toDouble(),
      status: _parseStatus(json['status'] as String),
      notes: json['notes'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
      items: rawItems
          .map((e) => BillingItemModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
