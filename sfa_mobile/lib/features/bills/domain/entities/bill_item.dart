import 'package:equatable/equatable.dart';

class BillItem extends Equatable {
  final int? id;
  final String clientBillId;
  final int productId;
  final String? productName;
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final bool isFreeIssue;
  final String billingItemType; // 'Sale' | 'Return'
  final String? returnType;     // 'Damage' | 'Expire' | 'MarketResell' — null for Sale
  final DateTime? expireDate;   // Only when returnType == 'Expire'
  final int lineNumber;

  const BillItem({
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

  @override
  List<Object?> get props => [
        id,
        clientBillId,
        productId,
        productName,
        quantity,
        unitPrice,
        discountRate,
        isFreeIssue,
        billingItemType,
        returnType,
        expireDate,
        lineNumber,
      ];
}
