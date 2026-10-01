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

  /// Sum of line discounts (subTotalAmount is net of these).
  final double itemWiseTotalDiscount;

  /// Line discounts + bill-level discount.
  final double totalDiscount;

  /// Outlet returns deducted from the total.
  final double returnValue;

  /// Informational only — not part of the arithmetic.
  final double freeIssueValue;
  final double freeIssueValueCompany;
  final double freeIssueValueDistributor;
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
    this.itemWiseTotalDiscount = 0,
    double? totalDiscount,
    this.returnValue = 0,
    this.freeIssueValue = 0,
    this.freeIssueValueCompany = 0,
    this.freeIssueValueDistributor = 0,
    required this.totalAmount,
    required this.status,
    this.notes,
    required this.createdAt,
    required this.items,
  }) : totalDiscount =
            totalDiscount ?? (itemWiseTotalDiscount + billDiscountAmount);

  /// Sales before any discount.
  double get grossAmount => subTotalAmount + itemWiseTotalDiscount;

  @override
  List<Object?> get props => [id];
}
