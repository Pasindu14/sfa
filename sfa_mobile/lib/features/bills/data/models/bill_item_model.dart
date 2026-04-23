import 'package:uswatte/features/bills/domain/entities/bill_item.dart';

class BillItemModel {
  final int? id;
  final String clientBillId;
  final int productId;
  final String? productName;
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final bool isFreeIssue;
  final String billingItemType;
  final String? returnType;
  final DateTime? expireDate;
  final int lineNumber;

  const BillItemModel({
    this.id,
    required this.clientBillId,
    required this.productId,
    this.productName,
    required this.quantity,
    required this.unitPrice,
    this.discountRate = 0,
    this.isFreeIssue = false,
    this.billingItemType = 'Sale',
    this.returnType,
    this.expireDate,
    required this.lineNumber,
  });

  factory BillItemModel.fromMap(Map<String, dynamic> map) => BillItemModel(
        id: map['id'] as int?,
        clientBillId: map['client_bill_id'] as String,
        productId: map['product_id'] as int,
        productName: map['product_name'] as String?,
        quantity: (map['quantity'] as num).toDouble(),
        unitPrice: (map['unit_price'] as num).toDouble(),
        discountRate: (map['discount_rate'] as num?)?.toDouble() ?? 0,
        isFreeIssue: (map['is_free_issue'] as int? ?? 0) == 1,
        billingItemType: map['billing_item_type'] as String? ?? 'Sale',
        returnType: map['return_type'] as String?,
        expireDate: map['expire_date'] != null
            ? DateTime.tryParse(map['expire_date'] as String)
            : null,
        lineNumber: map['line_number'] as int,
      );

  Map<String, dynamic> toMap() => {
        if (id != null) 'id': id,
        'client_bill_id': clientBillId,
        'product_id': productId,
        'quantity': quantity,
        'unit_price': unitPrice,
        'discount_rate': discountRate,
        'is_free_issue': isFreeIssue ? 1 : 0,
        'billing_item_type': billingItemType,
        'return_type': returnType,
        'expire_date': expireDate != null ? _dateOnly(expireDate!) : null,
        'line_number': lineNumber,
      };

  Map<String, dynamic> toCreateRequestJson() => {
        'productId': productId,
        'quantity': quantity,
        'unitPrice': unitPrice,
        'discountRate': discountRate,
        'isFreeIssue': isFreeIssue,
        'billingItemType': billingItemType,
        'returnType': returnType,
        'expireDate': expireDate != null ? _dateOnly(expireDate!) : null,
      };

  BillItem toEntity() => BillItem(
        id: id,
        clientBillId: clientBillId,
        productId: productId,
        productName: productName,
        quantity: quantity,
        unitPrice: unitPrice,
        discountRate: discountRate,
        isFreeIssue: isFreeIssue,
        billingItemType: billingItemType,
        returnType: returnType,
        expireDate: expireDate,
        lineNumber: lineNumber,
      );

  static String _dateOnly(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-'
      '${d.month.toString().padLeft(2, '0')}-'
      '${d.day.toString().padLeft(2, '0')}';
}
