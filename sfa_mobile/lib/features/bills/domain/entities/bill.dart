import 'package:equatable/equatable.dart';

import 'bill_item.dart';
import 'sync_status.dart';

/// A bill as it lives on the device. Server-side fields (serverBillId,
/// serverBillNumber) are null until the outbox flushes successfully.
class Bill extends Equatable {
  final String clientBillId;
  final int outletId;
  final DateTime billingDate;
  final double billDiscountRate;
  final double subTotalAmount;
  final double billDiscountAmount;
  final double totalAmount;
  final String? notes;
  final double? latitude;
  final double? longitude;
  final DateTime createdAt;
  final SyncStatus syncStatus;
  final int syncAttempts;
  final String? lastSyncError;
  final String? lastSyncErrorCode;
  final int? serverBillId;
  final String? serverBillNumber;
  final List<BillItem> items;

  const Bill({
    required this.clientBillId,
    required this.outletId,
    required this.billingDate,
    required this.billDiscountRate,
    required this.subTotalAmount,
    required this.billDiscountAmount,
    required this.totalAmount,
    this.notes,
    this.latitude,
    this.longitude,
    required this.createdAt,
    required this.syncStatus,
    this.syncAttempts = 0,
    this.lastSyncError,
    this.lastSyncErrorCode,
    this.serverBillId,
    this.serverBillNumber,
    this.items = const [],
  });

  @override
  List<Object?> get props => [
        clientBillId,
        outletId,
        billingDate,
        billDiscountRate,
        subTotalAmount,
        billDiscountAmount,
        totalAmount,
        notes,
        latitude,
        longitude,
        createdAt,
        syncStatus,
        syncAttempts,
        lastSyncError,
        lastSyncErrorCode,
        serverBillId,
        serverBillNumber,
        items,
      ];
}
