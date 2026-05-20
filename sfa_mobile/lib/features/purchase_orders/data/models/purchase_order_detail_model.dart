import '../../domain/entities/purchase_order_detail.dart';

const _statusOrdinal = {
  'Draft': 0,
  'PendingRepApproval': 1,
  'PendingManagerApproval': 2,
  'PendingDistributorFinalization': 3,
  'Finalized': 4,
  'Cancelled': 5,
  'PendingDistributorAcknowledgement': 6,
};

int _parseStatus(dynamic raw) =>
    raw is int ? raw : _statusOrdinal[raw as String] ?? 0;

class PurchaseOrderItemModel extends PurchaseOrderItem {
  const PurchaseOrderItemModel({
    required super.id,
    required super.productId,
    required super.productCode,
    required super.productDescription,
    required super.quantity,
    required super.unitPrice,
    required super.discount,
    required super.lineTotal,
  });

  factory PurchaseOrderItemModel.fromJson(Map<String, dynamic> json) {
    return PurchaseOrderItemModel(
      id: json['id'] as int,
      productId: json['productId'] as int,
      productCode: json['productCode'] as String,
      productDescription: json['productDescription'] as String,
      quantity: json['quantity'] as int,
      unitPrice: (json['unitPrice'] as num).toDouble(),
      discount: (json['discount'] as num).toDouble(),
      lineTotal: (json['lineTotal'] as num).toDouble(),
    );
  }
}

class PurchaseOrderHistoryEntryModel extends PurchaseOrderHistoryEntry {
  const PurchaseOrderHistoryEntryModel({
    required super.id,
    required super.action,
    super.performedByName,
    required super.performedAt,
    super.notes,
  });

  factory PurchaseOrderHistoryEntryModel.fromJson(Map<String, dynamic> json) {
    return PurchaseOrderHistoryEntryModel(
      id: json['id'] as int,
      action: json['action'] as String,
      performedByName: json['performedByName'] as String?,
      performedAt: DateTime.parse(json['performedAt'] as String),
      notes: json['notes'] as String?,
    );
  }
}

class PurchaseOrderDetailModel extends PurchaseOrderDetail {
  const PurchaseOrderDetailModel({
    required super.id,
    required super.orderNumber,
    required super.distributorId,
    required super.distributorName,
    required super.status,
    required super.totalAmount,
    super.notes,
    required super.items,
    required super.history,
    required super.createdAt,
    super.submittedAt,
    super.repApprovedAt,
  });

  factory PurchaseOrderDetailModel.fromJson(Map<String, dynamic> json) {
    return PurchaseOrderDetailModel(
      id: json['id'] as int,
      orderNumber: json['orderNumber'] as String,
      distributorId: json['distributorId'] as int,
      distributorName: json['distributorName'] as String,
      status: _parseStatus(json['status']),
      totalAmount: (json['totalAmount'] as num).toDouble(),
      notes: json['notes'] as String?,
      items: (json['items'] as List<dynamic>)
          .map((e) => PurchaseOrderItemModel.fromJson(e as Map<String, dynamic>))
          .toList(),
      history: (json['history'] as List<dynamic>)
          .map((e) => PurchaseOrderHistoryEntryModel.fromJson(e as Map<String, dynamic>))
          .toList(),
      createdAt: DateTime.parse(json['createdAt'] as String),
      submittedAt: json['submittedAt'] != null
          ? DateTime.parse(json['submittedAt'] as String)
          : null,
      repApprovedAt: json['repApprovedAt'] != null
          ? DateTime.parse(json['repApprovedAt'] as String)
          : null,
    );
  }
}
