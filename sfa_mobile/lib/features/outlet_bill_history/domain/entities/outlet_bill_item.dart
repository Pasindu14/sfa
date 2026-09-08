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

  /// Where this line came from: 'SalesRep' (the rep entered it) or 'DistributorReturn'
  /// (the system generated it when the distributor reduced a quantity).
  final String source;

  /// What the rep originally billed, when the distributor has since reduced this line.
  /// Null while the line has never been adjusted.
  final double? originalQuantity;

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
    this.source = 'SalesRep',
    this.originalQuantity,
    this.expireDate,
    required this.lineNumber,
  });

  /// True when the distributor reduced this line during review.
  bool get isAdjusted => originalQuantity != null && originalQuantity != quantity;

  /// True when this line is the mirror record of a quantity the distributor sent back.
  bool get isDistributorReturn => returnType == 'DistributorReturn';
}
