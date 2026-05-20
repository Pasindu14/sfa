class PurchaseOrderItem {
  final int id;
  final int productId;
  final String productCode;
  final String productDescription;
  final int quantity;
  final double unitPrice;
  final double discount;
  final double lineTotal;

  const PurchaseOrderItem({
    required this.id,
    required this.productId,
    required this.productCode,
    required this.productDescription,
    required this.quantity,
    required this.unitPrice,
    required this.discount,
    required this.lineTotal,
  });
}

class PurchaseOrderHistoryEntry {
  final int id;
  final String action;
  final String? performedByName;
  final DateTime performedAt;
  final String? notes;

  const PurchaseOrderHistoryEntry({
    required this.id,
    required this.action,
    this.performedByName,
    required this.performedAt,
    this.notes,
  });
}

class PurchaseOrderDetail {
  final int id;
  final String orderNumber;
  final int distributorId;
  final String distributorName;
  final int status;
  final double totalAmount;
  final String? notes;
  final List<PurchaseOrderItem> items;
  final List<PurchaseOrderHistoryEntry> history;
  final DateTime createdAt;
  final DateTime? submittedAt;
  final DateTime? repApprovedAt;

  const PurchaseOrderDetail({
    required this.id,
    required this.orderNumber,
    required this.distributorId,
    required this.distributorName,
    required this.status,
    required this.totalAmount,
    this.notes,
    required this.items,
    required this.history,
    required this.createdAt,
    this.submittedAt,
    this.repApprovedAt,
  });
}
