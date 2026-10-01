import 'dart:math' as math;

import 'package:uswatte/features/outlet_bill_history/data/models/outlet_bill_item_model.dart';
import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_detail.dart';

class OutletBillDetailModel {
  final int id;
  final String billingNumber;
  final String billingDate;
  final int outletId;
  final String outletName;
  final String salesRepName;
  final String distributorName;
  final double subTotalAmount;
  final double billDiscountRate;
  final double billDiscountAmount;
  final double totalAmount;
  final double itemWiseTotalDiscount;
  final double totalDiscount;
  final double returnValue;
  final double freeIssueValue;
  final double freeIssueValueCompany;
  final double freeIssueValueDistributor;
  final String repStatus;
  final String distributorStatus;
  final String? rejectionReason;
  final String? notes;
  final String createdAt;
  final String? lastAdjustedAt;
  final int adjustmentCount;
  final double distributorReturnValue;
  final int? pricingStructureId;
  final String? pricingStructureName;
  final List<OutletBillItemModel> items;

  const OutletBillDetailModel({
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

  factory OutletBillDetailModel.fromJson(Map<String, dynamic> json) {
    final subTotal = (json['subTotalAmount'] as num).toDouble();
    final billDiscount = (json['billDiscountAmount'] as num).toDouble();
    final total = (json['totalAmount'] as num).toDouble();
    final items = (json['items'] as List<dynamic>)
        .map((e) => OutletBillItemModel.fromJson(e as Map<String, dynamic>))
        .toList();

    // The breakdown fields are newer than the rest of the payload. When an
    // older API build omits them, rebuild them from what is always there so
    // Sales − Discount − Returns still adds up to the total:
    //   gross = subTotal + itemWise, returns = subTotal − billDiscount − total.
    final itemWise = (json['itemWiseTotalDiscount'] as num?)?.toDouble() ?? 0;
    final totalDiscount =
        (json['totalDiscount'] as num?)?.toDouble() ?? itemWise + billDiscount;
    final returnValue = (json['returnValue'] as num?)?.toDouble() ??
        math.max(0.0, subTotal - billDiscount - total);
    final freeIssueValue = (json['freeIssueValue'] as num?)?.toDouble() ??
        items
            .where((i) => i.isFreeIssue)
            .fold<double>(0, (s, i) => s + i.quantity * i.unitPrice);

    return OutletBillDetailModel(
        id: json['id'] as int,
        billingNumber: json['billingNumber'] as String,
        billingDate: json['billingDate'] as String,
        outletId: json['outletId'] as int,
        outletName: json['outletName'] as String,
        salesRepName: json['salesRepName'] as String,
        distributorName: json['distributorName'] as String,
        subTotalAmount: (json['subTotalAmount'] as num).toDouble(),
        billDiscountRate: (json['billDiscountRate'] as num).toDouble(),
        billDiscountAmount: (json['billDiscountAmount'] as num).toDouble(),
        totalAmount: total,
        itemWiseTotalDiscount: itemWise,
        totalDiscount: totalDiscount,
        returnValue: returnValue,
        freeIssueValue: freeIssueValue,
        freeIssueValueCompany:
            (json['freeIssueValueCompany'] as num?)?.toDouble() ?? 0,
        freeIssueValueDistributor:
            (json['freeIssueValueDistributor'] as num?)?.toDouble() ?? 0,
        repStatus: json['repStatus'] as String,
        distributorStatus: json['distributorStatus'] as String,
        rejectionReason: json['rejectionReason'] as String?,
        notes: json['notes'] as String?,
        createdAt: json['createdAt'] as String,
        // Tolerant of nulls so an older API build still parses.
        lastAdjustedAt: json['lastAdjustedAt'] as String?,
        adjustmentCount: (json['adjustmentCount'] as num?)?.toInt() ?? 0,
        distributorReturnValue:
            (json['distributorReturnValue'] as num?)?.toDouble() ?? 0,
        pricingStructureId: json['pricingStructureId'] as int?,
        pricingStructureName: json['pricingStructureName'] as String?,
        items: items,
    );
  }

  OutletBillDetail toEntity() => OutletBillDetail(
        id: id,
        billingNumber: billingNumber,
        billingDate: DateTime.parse(billingDate),
        outletId: outletId,
        outletName: outletName,
        salesRepName: salesRepName,
        distributorName: distributorName,
        subTotalAmount: subTotalAmount,
        billDiscountRate: billDiscountRate,
        billDiscountAmount: billDiscountAmount,
        totalAmount: totalAmount,
        itemWiseTotalDiscount: itemWiseTotalDiscount,
        totalDiscount: totalDiscount,
        returnValue: returnValue,
        freeIssueValue: freeIssueValue,
        freeIssueValueCompany: freeIssueValueCompany,
        freeIssueValueDistributor: freeIssueValueDistributor,
        repStatus: repStatus,
        distributorStatus: distributorStatus,
        rejectionReason: rejectionReason,
        notes: notes,
        createdAt: DateTime.parse(createdAt),
        lastAdjustedAt:
            lastAdjustedAt != null ? DateTime.parse(lastAdjustedAt!) : null,
        adjustmentCount: adjustmentCount,
        distributorReturnValue: distributorReturnValue,
        pricingStructureId: pricingStructureId,
        pricingStructureName: pricingStructureName,
        items: items.map((e) => e.toEntity()).toList(),
      );
}
