import 'package:equatable/equatable.dart';

/// Status names exactly as the API sends them. The API serializes every enum as
/// its member name, so these stay strings end to end — mapping them to ints
/// makes every comparison silently false the day the server adds a member.
abstract final class RouteUnlockStatus {
  static const pending = 'Pending';
  static const approved = 'Approved';
  static const rejected = 'Rejected';
  static const cancelled = 'Cancelled';
  static const revoked = 'Revoked';

  /// Read-side only ([RouteUnlockRequest.effectiveStatus]): a Pending request
  /// from a past day, or an Approved one past its `validTo`.
  static const expired = 'Expired';
}

/// A rep's request to lift the billing geofence on today's assigned route.
///
/// Decisions in the UI key off [effectiveStatus], never [status]: a request
/// the server still stores as Pending is dead once its day is over, and the
/// server is the one that knows which day that is.
class RouteUnlockRequest extends Equatable {
  final int id;
  final int userId;
  final String? userName;
  final String? loginName;
  final int routeId;
  final String? routeName;

  /// The Sri Lanka business date (`yyyy-MM-dd`) the request is for.
  final String businessDate;

  /// Stored status: Pending | Approved | Rejected | Cancelled | Revoked.
  final String status;

  /// [status] plus Expired.
  final String effectiveStatus;

  /// Approved and inside `[validFrom, validTo)` right now, per the server.
  final bool isCurrentlyEffective;

  final String requestReason;
  final DateTime requestedAt;
  final double? requestLatitude;
  final double? requestLongitude;
  final double? requestGpsAccuracyMeters;

  /// Who the request was routed to at request time. Null when the rep had no
  /// supervisor — only an admin can decide then.
  final int? supervisorUserId;
  final String? supervisorName;

  final int? reviewedByUserId;
  final String? reviewedByName;

  /// "Supervisor" | "Admin".
  final String? reviewedByRole;
  final DateTime? reviewedAt;

  /// The approve note or the reject reason.
  final String? reviewNote;

  final DateTime? validFrom;
  final DateTime? validTo;

  final int? revokedByUserId;
  final String? revokedByName;
  final DateTime? revokedAt;
  final String? revokeReason;

  final DateTime? cancelledAt;

  /// Optimistic-concurrency token; sent back on every transition.
  final int rowVersion;

  const RouteUnlockRequest({
    required this.id,
    required this.userId,
    this.userName,
    this.loginName,
    required this.routeId,
    this.routeName,
    required this.businessDate,
    required this.status,
    required this.effectiveStatus,
    required this.isCurrentlyEffective,
    required this.requestReason,
    required this.requestedAt,
    this.requestLatitude,
    this.requestLongitude,
    this.requestGpsAccuracyMeters,
    this.supervisorUserId,
    this.supervisorName,
    this.reviewedByUserId,
    this.reviewedByName,
    this.reviewedByRole,
    this.reviewedAt,
    this.reviewNote,
    this.validFrom,
    this.validTo,
    this.revokedByUserId,
    this.revokedByName,
    this.revokedAt,
    this.revokeReason,
    this.cancelledAt,
    required this.rowVersion,
  });

  bool get isPending => effectiveStatus == RouteUnlockStatus.pending;
  bool get isRejected => effectiveStatus == RouteUnlockStatus.rejected;
  bool get isRevoked => effectiveStatus == RouteUnlockStatus.revoked;
  bool get isCancelled => effectiveStatus == RouteUnlockStatus.cancelled;
  bool get isExpired => effectiveStatus == RouteUnlockStatus.expired;

  /// Approved and still live. [isCurrentlyEffective] is the server's view at
  /// response time; the [validTo] check lets a cached copy lapse on its own.
  bool get isLive {
    if (!isCurrentlyEffective) return false;
    final until = validTo;
    return until == null || DateTime.now().toUtc().isBefore(until.toUtc());
  }

  @override
  List<Object?> get props => [id, status, effectiveStatus, isCurrentlyEffective, rowVersion];
}
