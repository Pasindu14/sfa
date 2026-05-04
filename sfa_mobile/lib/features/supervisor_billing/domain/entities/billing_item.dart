import 'package:equatable/equatable.dart';

enum BillingItemType { sale, returnItem }

enum ReturnType { marketResell, damage, expire }

class BillingItem extends Equatable {
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
  final BillingItemType billingItemType;
  final ReturnType? returnType;
  final int lineNumber;

  const BillingItem({
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
    required this.lineNumber,
  });

  @override
  List<Object?> get props => [id];
}
