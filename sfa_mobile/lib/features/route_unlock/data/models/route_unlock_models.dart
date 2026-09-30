import '../../domain/entities/route_unlock_detail.dart';
import '../../domain/entities/route_unlock_request.dart';

// Every enum in these payloads (status, effectiveStatus, action, role) arrives
// as its member name — the API runs a global JsonStringEnumConverter — and is
// kept as that string. Nullable fields are read with `as T?` so a null from the
// server never becomes a cast error.

DateTime? _date(Object? raw) =>
    raw is String && raw.isNotEmpty ? DateTime.tryParse(raw) : null;

double? _double(Object? raw) => raw is num ? raw.toDouble() : null;

int? _int(Object? raw) => raw is num ? raw.toInt() : null;

class RouteUnlockRequestModel extends RouteUnlockRequest {
  const RouteUnlockRequestModel({
    required super.id,
    required super.userId,
    super.userName,
    super.loginName,
    required super.routeId,
    super.routeName,
    required super.businessDate,
    required super.status,
    required super.effectiveStatus,
    required super.isCurrentlyEffective,
    required super.requestReason,
    required super.requestedAt,
    super.requestLatitude,
    super.requestLongitude,
    super.requestGpsAccuracyMeters,
    super.supervisorUserId,
    super.supervisorName,
    super.reviewedByUserId,
    super.reviewedByName,
    super.reviewedByRole,
    super.reviewedAt,
    super.reviewNote,
    super.validFrom,
    super.validTo,
    super.revokedByUserId,
    super.revokedByName,
    super.revokedAt,
    super.revokeReason,
    super.cancelledAt,
    required super.rowVersion,
  });

  factory RouteUnlockRequestModel.fromJson(Map<String, dynamic> json) {
    final status = json['status'] as String;
    return RouteUnlockRequestModel(
      id: (json['id'] as num).toInt(),
      userId: (json['userId'] as num).toInt(),
      userName: json['userName'] as String?,
      loginName: json['loginName'] as String?,
      routeId: (json['routeId'] as num).toInt(),
      routeName: json['routeName'] as String?,
      businessDate: json['businessDate'] as String? ?? '',
      status: status,
      // An older server without the read-side field: fall back to the stored
      // status rather than inventing an Expired the server never said.
      effectiveStatus: json['effectiveStatus'] as String? ?? status,
      isCurrentlyEffective: json['isCurrentlyEffective'] as bool? ?? false,
      requestReason: json['requestReason'] as String? ?? '',
      requestedAt: _date(json['requestedAt']) ?? DateTime.now().toUtc(),
      requestLatitude: _double(json['requestLatitude']),
      requestLongitude: _double(json['requestLongitude']),
      requestGpsAccuracyMeters: _double(json['requestGpsAccuracyMeters']),
      supervisorUserId: _int(json['supervisorUserId']),
      supervisorName: json['supervisorName'] as String?,
      reviewedByUserId: _int(json['reviewedByUserId']),
      reviewedByName: json['reviewedByName'] as String?,
      reviewedByRole: json['reviewedByRole'] as String?,
      reviewedAt: _date(json['reviewedAt']),
      reviewNote: json['reviewNote'] as String?,
      validFrom: _date(json['validFrom']),
      validTo: _date(json['validTo']),
      revokedByUserId: _int(json['revokedByUserId']),
      revokedByName: json['revokedByName'] as String?,
      revokedAt: _date(json['revokedAt']),
      revokeReason: json['revokeReason'] as String?,
      cancelledAt: _date(json['cancelledAt']),
      rowVersion: _int(json['rowVersion']) ?? 0,
    );
  }
}

class RouteUnlockEventModel extends RouteUnlockEvent {
  const RouteUnlockEventModel({
    required super.id,
    required super.action,
    super.fromStatus,
    required super.toStatus,
    required super.performedByUserId,
    super.performedByName,
    required super.performedByRole,
    required super.performedAt,
    super.note,
  });

  factory RouteUnlockEventModel.fromJson(Map<String, dynamic> json) =>
      RouteUnlockEventModel(
        id: (json['id'] as num).toInt(),
        action: json['action'] as String,
        fromStatus: json['fromStatus'] as String?,
        toStatus: json['toStatus'] as String? ?? '',
        performedByUserId: _int(json['performedByUserId']) ?? 0,
        performedByName: json['performedByName'] as String?,
        performedByRole: json['performedByRole'] as String? ?? '',
        performedAt: _date(json['performedAt']) ?? DateTime.now().toUtc(),
        note: json['note'] as String?,
      );
}

class RouteUnlockBillModel extends RouteUnlockBill {
  const RouteUnlockBillModel({
    required super.billingId,
    super.billingNumber,
    super.billingDate,
    required super.outletId,
    required super.outletName,
    super.distanceFromOutletMeters,
    required super.totalAmount,
    super.createdAt,
  });

  factory RouteUnlockBillModel.fromJson(Map<String, dynamic> json) =>
      RouteUnlockBillModel(
        billingId: (json['billingId'] as num).toInt(),
        billingNumber: json['billingNumber'] as String?,
        billingDate: _date(json['billingDate']),
        outletId: _int(json['outletId']) ?? 0,
        outletName: json['outletName'] as String? ?? '',
        distanceFromOutletMeters: _double(json['distanceFromOutletMeters']),
        totalAmount: _double(json['totalAmount']) ?? 0,
        createdAt: _date(json['createdAt']),
      );
}

class RouteUnlockDetailModel extends RouteUnlockDetail {
  const RouteUnlockDetailModel({
    required super.request,
    required super.events,
    required super.bills,
  });

  factory RouteUnlockDetailModel.fromJson(Map<String, dynamic> json) {
    List<Map<String, dynamic>> list(Object? raw) => raw is List
        ? raw.cast<Map<String, dynamic>>()
        : const <Map<String, dynamic>>[];

    final events = list(json['events']).map(RouteUnlockEventModel.fromJson).toList()
      // The contract says oldest first; sorting here keeps the timeline right
      // even if a server build forgets the ORDER BY.
      ..sort((a, b) => a.performedAt.compareTo(b.performedAt));

    return RouteUnlockDetailModel(
      request: RouteUnlockRequestModel.fromJson(
          json['request'] as Map<String, dynamic>),
      events: events,
      bills: list(json['bills']).map(RouteUnlockBillModel.fromJson).toList(),
    );
  }
}
