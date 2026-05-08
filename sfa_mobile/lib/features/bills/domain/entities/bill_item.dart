import 'package:equatable/equatable.dart';

class BillItem extends Equatable {
  final int? id;
  final String clientBillId;
  final int productId;
  final String? productName;
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final String billingItemType; // 'Sale' | 'FreeIssue' | 'Return'
  final String? returnType;     // 'Damage' | 'Expire' | 'MarketResell' — null for Sale and FreeIssue
  final String? freeIssueSource; // 'Company' | 'Distributor' — only set when billingItemType == 'FreeIssue'
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
    this.billingItemType = 'Sale',
    this.returnType,
    this.freeIssueSource,
    this.expireDate,
    required this.lineNumber,
  });

  bool get isFreeIssue => billingItemType == 'FreeIssue';
  bool get isReturn    => billingItemType == 'Return';
  bool get isSale      => billingItemType == 'Sale';

  @override
  List<Object?> get props => [
        id,
        clientBillId,
        productId,
        productName,
        quantity,
        unitPrice,
        discountRate,
        billingItemType,
        returnType,
        freeIssueSource,
        expireDate,
        lineNumber,
      ];
}
