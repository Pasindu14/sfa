import 'package:uswatte/features/supervisor_billing/domain/entities/billing_item.dart';

class BillingItemModel extends BillingItem {
  const BillingItemModel({
    required super.id,
    required super.productId,
    required super.productCode,
    required super.productDescription,
    required super.quantity,
    required super.unitPrice,
    required super.discountRate,
    required super.discountAmount,
    required super.totalPrice,
    required super.isFreeIssue,
    required super.billingItemType,
    super.returnType,
    required super.lineNumber,
  });

  factory BillingItemModel.fromJson(Map<String, dynamic> json) {
    return BillingItemModel(
      id: json['id'] as int,
      productId: json['productId'] as int,
      productCode: json['productCode'] as String,
      productDescription: json['productDescription'] as String,
      quantity: (json['quantity'] as num).toDouble(),
      unitPrice: (json['unitPrice'] as num).toDouble(),
      discountRate: (json['discountRate'] as num).toDouble(),
      discountAmount: (json['discountAmount'] as num).toDouble(),
      totalPrice: (json['totalPrice'] as num).toDouble(),
      isFreeIssue: json['isFreeIssue'] as bool,
      billingItemType: _parseItemType(json['billingItemType'] as String),
      returnType: json['returnType'] != null
          ? _parseReturnType(json['returnType'] as String)
          : null,
      lineNumber: json['lineNumber'] as int,
    );
  }

  static BillingItemType _parseItemType(String s) {
    return s.toLowerCase() == 'return'
        ? BillingItemType.returnItem
        : BillingItemType.sale;
  }

  static ReturnType _parseReturnType(String s) {
    switch (s.toLowerCase()) {
      case 'damage':
        return ReturnType.damage;
      case 'expire':
        return ReturnType.expire;
      default:
        return ReturnType.marketResell;
    }
  }
}
