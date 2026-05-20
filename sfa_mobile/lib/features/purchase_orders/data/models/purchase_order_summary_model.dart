import '../../domain/entities/purchase_order_summary.dart';

// The API serializes PurchaseOrderStatus as a string name (JsonStringEnumConverter).
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

class PurchaseOrderSummaryModel extends PurchaseOrderSummary {
  const PurchaseOrderSummaryModel({
    required super.id,
    required super.orderNumber,
    required super.distributorId,
    required super.distributorName,
    required super.status,
    required super.totalAmount,
    required super.itemCount,
    required super.createdAt,
    super.submittedAt,
  });

  factory PurchaseOrderSummaryModel.fromJson(Map<String, dynamic> json) {
    return PurchaseOrderSummaryModel(
      id: json['id'] as int,
      orderNumber: json['orderNumber'] as String,
      distributorId: json['distributorId'] as int,
      distributorName: json['distributorName'] as String,
      status: _parseStatus(json['status']),
      totalAmount: (json['totalAmount'] as num).toDouble(),
      itemCount: json['itemCount'] as int,
      createdAt: DateTime.parse(json['createdAt'] as String),
      submittedAt: json['submittedAt'] != null
          ? DateTime.parse(json['submittedAt'] as String)
          : null,
    );
  }
}
