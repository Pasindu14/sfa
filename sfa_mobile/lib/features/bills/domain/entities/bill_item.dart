import 'package:equatable/equatable.dart';

class BillItem extends Equatable {
  final int? id;
  final String clientBillId;
  final int productId;
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final bool isFreeIssue;
  final int lineNumber;

  const BillItem({
    this.id,
    required this.clientBillId,
    required this.productId,
    required this.quantity,
    required this.unitPrice,
    this.discountRate = 0,
    this.isFreeIssue = false,
    required this.lineNumber,
  });

  @override
  List<Object?> get props => [
        id,
        clientBillId,
        productId,
        quantity,
        unitPrice,
        discountRate,
        isFreeIssue,
        lineNumber,
      ];
}
