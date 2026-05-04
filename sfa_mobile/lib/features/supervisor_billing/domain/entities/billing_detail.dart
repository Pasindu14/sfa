import 'package:equatable/equatable.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_item.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';

class BillingDetail extends Equatable {
  final int id;
  final String billingNumber;
  final String billingDate;
  final int outletId;
  final String outletName;
  final int salesRepId;
  final String salesRepName;
  final int distributorId;
  final String distributorName;
  final String? supervisorName;
  final double subTotalAmount;
  final double billDiscountRate;
  final double billDiscountAmount;
  final double totalAmount;
  final BillingStatus status;
  final String? notes;
  final DateTime createdAt;
  final List<BillingItem> items;

  const BillingDetail({
    required this.id,
    required this.billingNumber,
    required this.billingDate,
    required this.outletId,
    required this.outletName,
    required this.salesRepId,
    required this.salesRepName,
    required this.distributorId,
    required this.distributorName,
    this.supervisorName,
    required this.subTotalAmount,
    required this.billDiscountRate,
    required this.billDiscountAmount,
    required this.totalAmount,
    required this.status,
    this.notes,
    required this.createdAt,
    required this.items,
  });

  @override
  List<Object?> get props => [id];
}
