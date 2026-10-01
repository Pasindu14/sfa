import 'package:uswatte/core/utils/bill_breakdown.dart';
import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_item.dart';

class OutletBillDetail {
  final int id;
  final String billingNumber;
  final DateTime billingDate;
  final int outletId;
  final String outletName;
  final String salesRepName;
  final String distributorName;
  final double subTotalAmount;
  final double billDiscountRate;
  final double billDiscountAmount;
  final double totalAmount;

  /// Discounts given on the sale lines. [subTotalAmount] is already net of these.
  final double itemWiseTotalDiscount;

  /// Line discounts plus the bill-level discount.
  final double totalDiscount;

  /// Outlet returns deducted from the total.
  final double returnValue;

  /// Informational free-issue value (not part of the arithmetic), and its source split.
  final double freeIssueValue;
  final double freeIssueValueCompany;
  final double freeIssueValueDistributor;
  final String repStatus;
  final String distributorStatus;
  final String? rejectionReason;
  final String? notes;
  final DateTime createdAt;

  /// When the distributor last reduced a quantity on this bill. Null if never adjusted.
  final DateTime? lastAdjustedAt;

  /// How many times the distributor has adjusted this bill.
  final int adjustmentCount;

  /// Value of the quantities the distributor sent back. Informational — already reflected in
  /// [totalAmount] via the reduced sale lines, never subtracted a second time.
  final double distributorReturnValue;

  /// The structure selected when the bill was submitted. Lines may differ.
  final int? pricingStructureId;
  final String? pricingStructureName;

  final List<OutletBillItem> items;

  const OutletBillDetail({
    required this.id,
    required this.billingNumber,
    required this.billingDate,
    required this.outletId,
    required this.outletName,
    required this.salesRepName,
    required this.distributorName,
    required this.subTotalAmount,
    required this.billDiscountRate,
    required this.billDiscountAmount,
    required this.totalAmount,
    this.itemWiseTotalDiscount = 0,
    this.totalDiscount = 0,
    this.returnValue = 0,
    this.freeIssueValue = 0,
    this.freeIssueValueCompany = 0,
    this.freeIssueValueDistributor = 0,
    required this.repStatus,
    required this.distributorStatus,
    this.rejectionReason,
    this.notes,
    required this.createdAt,
    this.lastAdjustedAt,
    this.adjustmentCount = 0,
    this.distributorReturnValue = 0,
    this.pricingStructureId,
    this.pricingStructureName,
    required this.items,
  });

  bool get isAdjustedByDistributor => adjustmentCount > 0;

  /// Sales (gross) − Discount − Returns = Total.
  BillBreakdown get breakdown => BillBreakdown(
        gross: subTotalAmount + itemWiseTotalDiscount,
        discount: totalDiscount,
        returns: returnValue,
        total: totalAmount,
        freeIssue: freeIssueValue,
        freeIssueCompany: freeIssueValueCompany,
        freeIssueDistributor: freeIssueValueDistributor,
      );
}
