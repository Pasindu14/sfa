import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_request.dart';

/// A request as the server would send it; [live] marks an approval that is in
/// force right now.
RouteUnlockRequest unlock({
  int id = 12,
  String status = RouteUnlockStatus.pending,
  String? effectiveStatus,
  bool live = false,
  String? supervisorName = 'Nimal S',
  String? reviewedByName,
  String? reviewNote,
  int rowVersion = 1,
}) =>
    RouteUnlockRequest(
      id: id,
      userId: 7,
      routeId: 3,
      routeName: 'Kandy Town A',
      businessDate: '2026-09-30',
      status: status,
      effectiveStatus: effectiveStatus ?? status,
      isCurrentlyEffective: live,
      requestReason: 'GPS not accurate',
      requestedAt: DateTime.utc(2026, 9, 30, 3, 10),
      supervisorName: supervisorName,
      reviewedByName: reviewedByName,
      reviewNote: reviewNote,
      validTo: live ? DateTime.now().toUtc().add(const Duration(hours: 6)) : null,
      rowVersion: rowVersion,
    );
