import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/not_billings/domain/entities/not_billing.dart';
import 'package:uswatte/features/not_billings/domain/entities/not_billing_reason.dart';

class NotBillingModel {
  final String clientNotBillingId;
  final int outletId;
  final String? outletName;
  final String? routeName;
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

  const NotBillingModel({
    required this.clientNotBillingId,
    required this.outletId,
    this.outletName,
    this.routeName,
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

  factory NotBillingModel.fromMap(Map<String, dynamic> map) => NotBillingModel(
        clientNotBillingId: map['client_not_billing_id'] as String,
        outletId: map['outlet_id'] as int,
        outletName: map['outlet_name'] as String?,
        routeName: map['route_name'] as String?,
        notBillingDate: DateTime.parse(map['not_billing_date'] as String),
        reason: NotBillingReason.fromApi(map['reason'] as String),
        notes: map['notes'] as String?,
        createdAt: DateTime.parse(map['created_at'] as String),
        syncStatus: SyncStatusX.fromDb(map['sync_status'] as String),
        syncAttempts: map['sync_attempts'] as int? ?? 0,
        lastSyncError: map['last_sync_error'] as String?,
        lastSyncErrorCode: map['last_sync_error_code'] as String?,
        serverNotBillingId: map['server_not_billing_id'] as int?,
        serverNotBillingNumber: map['server_not_billing_number'] as String?,
      );

  Map<String, dynamic> toMap() => {
        'client_not_billing_id': clientNotBillingId,
        'outlet_id': outletId,
        'outlet_name': outletName,
        'route_name': routeName,
        'not_billing_date': _dateOnly(notBillingDate),
        'reason': reason.apiValue,
        'notes': notes,
        'created_at': createdAt.toIso8601String(),
        'sync_status': syncStatus.dbValue,
        'sync_attempts': syncAttempts,
        'last_sync_error': lastSyncError,
        'last_sync_error_code': lastSyncErrorCode,
        'server_not_billing_id': serverNotBillingId,
        'server_not_billing_number': serverNotBillingNumber,
      };

  Map<String, dynamic> toCreateRequestJson() => {
        'outletId': outletId,
        'notBillingDate': _dateOnly(notBillingDate),
        'reason': reason.apiValue,
        'notes': notes,
      };

  NotBilling toEntity() => NotBilling(
        clientNotBillingId: clientNotBillingId,
        outletId: outletId,
        outletName: outletName,
        routeName: routeName,
        notBillingDate: notBillingDate,
        reason: reason,
        notes: notes,
        createdAt: createdAt,
        syncStatus: syncStatus,
        syncAttempts: syncAttempts,
        lastSyncError: lastSyncError,
        lastSyncErrorCode: lastSyncErrorCode,
        serverNotBillingId: serverNotBillingId,
        serverNotBillingNumber: serverNotBillingNumber,
      );

  NotBillingModel copyWith({
    String? outletName,
    String? routeName,
    SyncStatus? syncStatus,
    int? syncAttempts,
    String? lastSyncError,
    String? lastSyncErrorCode,
    int? serverNotBillingId,
    String? serverNotBillingNumber,
  }) =>
      NotBillingModel(
        clientNotBillingId: clientNotBillingId,
        outletId: outletId,
        outletName: outletName ?? this.outletName,
        routeName: routeName ?? this.routeName,
        notBillingDate: notBillingDate,
        reason: reason,
        notes: notes,
        createdAt: createdAt,
        syncStatus: syncStatus ?? this.syncStatus,
        syncAttempts: syncAttempts ?? this.syncAttempts,
        lastSyncError: lastSyncError ?? this.lastSyncError,
        lastSyncErrorCode: lastSyncErrorCode ?? this.lastSyncErrorCode,
        serverNotBillingId: serverNotBillingId ?? this.serverNotBillingId,
        serverNotBillingNumber: serverNotBillingNumber ?? this.serverNotBillingNumber,
      );

  static String _dateOnly(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-'
      '${d.month.toString().padLeft(2, '0')}-'
      '${d.day.toString().padLeft(2, '0')}';
}
