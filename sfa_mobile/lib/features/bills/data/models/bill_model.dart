import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';

import 'bill_item_model.dart';

class BillModel {
  final String clientBillId;
  final int outletId;
  final String billingType;
  final String? returnType;
  final int? originalBillId;
  final DateTime billingDate;
  final double billDiscountRate;
  final double subTotalAmount;
  final double billDiscountAmount;
  final double totalAmount;
  final String? notes;
  final DateTime createdAt;
  final SyncStatus syncStatus;
  final int syncAttempts;
  final String? lastSyncError;
  final String? lastSyncErrorCode;
  final int? serverBillId;
  final String? serverBillNumber;
  final List<BillItemModel> items;

  const BillModel({
    required this.clientBillId,
    required this.outletId,
    required this.billingType,
    this.returnType,
    this.originalBillId,
    required this.billingDate,
    required this.billDiscountRate,
    required this.subTotalAmount,
    required this.billDiscountAmount,
    required this.totalAmount,
    this.notes,
    required this.createdAt,
    required this.syncStatus,
    this.syncAttempts = 0,
    this.lastSyncError,
    this.lastSyncErrorCode,
    this.serverBillId,
    this.serverBillNumber,
    this.items = const [],
  });

  factory BillModel.fromMap(Map<String, dynamic> map, List<BillItemModel> items) => BillModel(
        clientBillId: map['client_bill_id'] as String,
        outletId: map['outlet_id'] as int,
        billingType: map['billing_type'] as String,
        returnType: map['return_type'] as String?,
        originalBillId: map['original_bill_id'] as int?,
        billingDate: DateTime.parse(map['billing_date'] as String),
        billDiscountRate: (map['bill_discount_rate'] as num).toDouble(),
        subTotalAmount: (map['sub_total_amount'] as num).toDouble(),
        billDiscountAmount: (map['bill_discount_amount'] as num).toDouble(),
        totalAmount: (map['total_amount'] as num).toDouble(),
        notes: map['notes'] as String?,
        createdAt: DateTime.parse(map['created_at'] as String),
        syncStatus: SyncStatusX.fromDb(map['sync_status'] as String),
        syncAttempts: map['sync_attempts'] as int? ?? 0,
        lastSyncError: map['last_sync_error'] as String?,
        lastSyncErrorCode: map['last_sync_error_code'] as String?,
        serverBillId: map['server_bill_id'] as int?,
        serverBillNumber: map['server_bill_number'] as String?,
        items: items,
      );

  Map<String, dynamic> toMap() => {
        'client_bill_id': clientBillId,
        'outlet_id': outletId,
        'billing_type': billingType,
        'return_type': returnType,
        'original_bill_id': originalBillId,
        'billing_date': _dateOnly(billingDate),
        'bill_discount_rate': billDiscountRate,
        'sub_total_amount': subTotalAmount,
        'bill_discount_amount': billDiscountAmount,
        'total_amount': totalAmount,
        'notes': notes,
        'created_at': createdAt.toIso8601String(),
        'sync_status': syncStatus.dbValue,
        'sync_attempts': syncAttempts,
        'last_sync_error': lastSyncError,
        'last_sync_error_code': lastSyncErrorCode,
        'server_bill_id': serverBillId,
        'server_bill_number': serverBillNumber,
      };

  /// Payload sent to POST /api/v1/billings. Matches CreateBillingRequest on the server.
  /// The client_bill_id is used as the X-Idempotency-Key header, not in the body.
  Map<String, dynamic> toCreateRequestJson() => {
        'outletId': outletId,
        'billingType': billingType,
        'returnType': returnType,
        'originalBillingId': originalBillId,
        'billingDate': _dateOnly(billingDate),
        'billDiscountRate': billDiscountRate,
        'notes': notes,
        'items': items.map((i) => i.toCreateRequestJson()).toList(),
      };

  Bill toEntity() => Bill(
        clientBillId: clientBillId,
        outletId: outletId,
        billingType: billingType,
        returnType: returnType,
        originalBillId: originalBillId,
        billingDate: billingDate,
        billDiscountRate: billDiscountRate,
        subTotalAmount: subTotalAmount,
        billDiscountAmount: billDiscountAmount,
        totalAmount: totalAmount,
        notes: notes,
        createdAt: createdAt,
        syncStatus: syncStatus,
        syncAttempts: syncAttempts,
        lastSyncError: lastSyncError,
        lastSyncErrorCode: lastSyncErrorCode,
        serverBillId: serverBillId,
        serverBillNumber: serverBillNumber,
        items: items.map((i) => i.toEntity()).toList(),
      );

  BillModel copyWith({
    SyncStatus? syncStatus,
    int? syncAttempts,
    String? lastSyncError,
    String? lastSyncErrorCode,
    int? serverBillId,
    String? serverBillNumber,
  }) =>
      BillModel(
        clientBillId: clientBillId,
        outletId: outletId,
        billingType: billingType,
        returnType: returnType,
        originalBillId: originalBillId,
        billingDate: billingDate,
        billDiscountRate: billDiscountRate,
        subTotalAmount: subTotalAmount,
        billDiscountAmount: billDiscountAmount,
        totalAmount: totalAmount,
        notes: notes,
        createdAt: createdAt,
        syncStatus: syncStatus ?? this.syncStatus,
        syncAttempts: syncAttempts ?? this.syncAttempts,
        lastSyncError: lastSyncError ?? this.lastSyncError,
        lastSyncErrorCode: lastSyncErrorCode ?? this.lastSyncErrorCode,
        serverBillId: serverBillId ?? this.serverBillId,
        serverBillNumber: serverBillNumber ?? this.serverBillNumber,
        items: items,
      );

  static String _dateOnly(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-'
      '${d.month.toString().padLeft(2, '0')}-'
      '${d.day.toString().padLeft(2, '0')}';
}
