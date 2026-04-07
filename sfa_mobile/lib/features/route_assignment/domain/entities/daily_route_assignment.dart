import 'package:equatable/equatable.dart';

enum DeletionStatus { none, pendingApproval, approved, rejected }

class DailyRouteAssignment extends Equatable {
  final int id;
  final int userId;
  final String userName;
  final int routeId;
  final String routeName;
  final DateTime assignedDate;
  final bool isActive;
  final DateTime createdAt;
  final DeletionStatus deletionStatus;
  final DateTime? deletionRequestedAt;
  final String? deletionRequestReason;
  final String? deletionRejectionReason;

  const DailyRouteAssignment({
    required this.id,
    required this.userId,
    required this.userName,
    required this.routeId,
    required this.routeName,
    required this.assignedDate,
    required this.isActive,
    required this.createdAt,
    this.deletionStatus = DeletionStatus.none,
    this.deletionRequestedAt,
    this.deletionRequestReason,
    this.deletionRejectionReason,
  });

  @override
  List<Object?> get props =>
      [id, userId, routeId, assignedDate, isActive, deletionStatus];
}

class AssignmentsResult {
  final List<DailyRouteAssignment> assignments;
  final int totalCount;
  final int page;
  final int pageSize;

  const AssignmentsResult({
    required this.assignments,
    required this.totalCount,
    required this.page,
    required this.pageSize,
  });
}
