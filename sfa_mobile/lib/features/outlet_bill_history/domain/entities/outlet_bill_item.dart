class OutletBillItem {
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
  final DateTime? expireDate;
  final int lineNumber;

  const OutletBillItem({
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
    this.expireDate,
    required this.lineNumber,
  });
}
