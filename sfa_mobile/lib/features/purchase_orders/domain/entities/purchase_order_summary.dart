class PurchaseOrderSummary {
  final int id;
  final String orderNumber;
  final int distributorId;
  final String distributorName;
  final int status;
  final double totalAmount;
  final int itemCount;
  final DateTime createdAt;
  final DateTime? submittedAt;

  const PurchaseOrderSummary({
    required this.id,
    required this.orderNumber,
    required this.distributorId,
    required this.distributorName,
    required this.status,
    required this.totalAmount,
    required this.itemCount,
    required this.createdAt,
    this.submittedAt,
  });
}
