import 'package:equatable/equatable.dart';

class DailyRouteAssignment extends Equatable {
  final int id;
  final int userId;
  final String userName;
  final int routeId;
  final String routeName;
  final DateTime assignedDate;
  final bool isActive;
  final DateTime createdAt;

  const DailyRouteAssignment({
    required this.id,
    required this.userId,
    required this.userName,
    required this.routeId,
    required this.routeName,
    required this.assignedDate,
    required this.isActive,
    required this.createdAt,
  });

  @override
  List<Object?> get props =>
      [id, userId, routeId, assignedDate, isActive];
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
