import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_item.dart';

class OutletBillItemModel {
  final int id;
  final int productId;
  final String productCode;
  final String productDescription;
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final double discountAmount;
  final double totalPrice;
  final bool isFreeIssue;
  final String billingItemType;
  final String? returnType;
  final String source;
  final double? originalQuantity;
  final String? expireDate;
  final int lineNumber;

  const OutletBillItemModel({
    required this.id,
    required this.productId,
    required this.productCode,
    required this.productDescription,
    required this.quantity,
    required this.unitPrice,
    required this.discountRate,
    required this.discountAmount,
    required this.totalPrice,
    required this.isFreeIssue,
    required this.billingItemType,
    this.returnType,
    this.source = 'SalesRep',
    this.originalQuantity,
    this.expireDate,
    required this.lineNumber,
  });

  factory OutletBillItemModel.fromJson(Map<String, dynamic> json) =>
      OutletBillItemModel(
        id: json['id'] as int,
        productId: json['productId'] as int,
        productCode: json['productCode'] as String,
        productDescription: json['productDescription'] as String,
        quantity: (json['quantity'] as num).toDouble(),
        unitPrice: (json['unitPrice'] as num).toDouble(),
        discountRate: (json['discountRate'] as num).toDouble(),
        discountAmount: (json['discountAmount'] as num).toDouble(),
        totalPrice: (json['totalPrice'] as num).toDouble(),
        isFreeIssue: (json['billingItemType'] as String) == 'FreeIssue',
        billingItemType: json['billingItemType'] as String,
        returnType: json['returnType'] as String?,
        // Defaulted rather than required so an older API build (which sends neither field)
        // still parses instead of crashing the bill detail screen.
        source: json['source'] as String? ?? 'SalesRep',
        originalQuantity: (json['originalQuantity'] as num?)?.toDouble(),
        expireDate: json['expireDate'] as String?,
        lineNumber: json['lineNumber'] as int,
      );

  OutletBillItem toEntity() => OutletBillItem(
        id: id,
        productId: productId,
        productCode: productCode,
        productDescription: productDescription,
        quantity: quantity,
        unitPrice: unitPrice,
        discountRate: discountRate,
        discountAmount: discountAmount,
        totalPrice: totalPrice,
        isFreeIssue: isFreeIssue,
        billingItemType: billingItemType,
        returnType: returnType,
        source: source,
        originalQuantity: originalQuantity,
        expireDate: expireDate != null ? DateTime.parse(expireDate!) : null,
        lineNumber: lineNumber,
      );
}
