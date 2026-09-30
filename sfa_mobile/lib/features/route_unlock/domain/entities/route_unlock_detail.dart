import 'route_unlock_request.dart';

/// One append-only entry in a request's audit trail.
class RouteUnlockEvent {
  final int id;

  /// Requested | Approved | Rejected | Cancelled | Revoked.
  final String action;
  final String? fromStatus;
  final String toStatus;
  final int performedByUserId;
  final String? performedByName;
  final String performedByRole;
  final DateTime performedAt;
  final String? note;

  const RouteUnlockEvent({
    required this.id,
    required this.action,
    this.fromStatus,
    required this.toStatus,
    required this.performedByUserId,
    this.performedByName,
    required this.performedByRole,
    required this.performedAt,
    this.note,
  });
}

/// A bill the server stamped with this unlock — only bills that were genuinely
/// out of range, so this list is exactly what the unlock was used for.
class RouteUnlockBill {
  final int billingId;
  final String? billingNumber;
  final DateTime? billingDate;
  final int outletId;
  final String outletName;
  final double? distanceFromOutletMeters;
  final double totalAmount;
  final DateTime? createdAt;

  const RouteUnlockBill({
    required this.billingId,
    this.billingNumber,
    this.billingDate,
    required this.outletId,
    required this.outletName,
    this.distanceFromOutletMeters,
    required this.totalAmount,
    this.createdAt,
  });
}

class RouteUnlockDetail {
  final RouteUnlockRequest request;

  /// Oldest first.
  final List<RouteUnlockEvent> events;
  final List<RouteUnlockBill> bills;

  const RouteUnlockDetail({
    required this.request,
    required this.events,
    required this.bills,
  });
}
