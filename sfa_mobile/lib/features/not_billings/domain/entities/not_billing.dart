import 'package:equatable/equatable.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/not_billings/domain/entities/not_billing_reason.dart';

/// A not-billing record as it lives on the device.
/// Server-side fields are null until the outbox flushes successfully.
class NotBilling extends Equatable {
  final String clientNotBillingId;
  final int outletId;
  final DateTime notBillingDate;
  final NotBillingReason reason;
  final String? notes;
  final DateTime createdAt;
  final SyncStatus syncStatus;
  final int syncAttempts;
  final String? lastSyncError;
  final String? lastSyncErrorCode;
  final int? serverNotBillingId;
  final String? serverNotBillingNumber;

  const NotBilling({
    required this.clientNotBillingId,
    required this.outletId,
    required this.notBillingDate,
    required this.reason,
    this.notes,
    required this.createdAt,
    required this.syncStatus,
    this.syncAttempts = 0,
    this.lastSyncError,
    this.lastSyncErrorCode,
    this.serverNotBillingId,
    this.serverNotBillingNumber,
  });

  @override
  List<Object?> get props => [
        clientNotBillingId,
        outletId,
        notBillingDate,
        reason,
        notes,
        createdAt,
        syncStatus,
        syncAttempts,
        lastSyncError,
        lastSyncErrorCode,
        serverNotBillingId,
        serverNotBillingNumber,
      ];
}
