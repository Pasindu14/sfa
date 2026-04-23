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
  final String status;
  final String? notes;
  final String createdAt;
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
    required this.status,
    this.notes,
    required this.createdAt,
    required this.items,
  });

  factory OutletBillDetailModel.fromJson(Map<String, dynamic> json) =>
      OutletBillDetailModel(
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
        totalAmount: (json['totalAmount'] as num).toDouble(),
        status: json['status'] as String,
        notes: json['notes'] as String?,
        createdAt: json['createdAt'] as String,
        items: (json['items'] as List<dynamic>)
            .map((e) => OutletBillItemModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

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
        status: status,
        notes: notes,
        createdAt: DateTime.parse(createdAt),
        items: items.map((e) => e.toEntity()).toList(),
      );
}
