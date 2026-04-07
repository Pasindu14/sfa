import 'package:uswatte/features/route_assignment/domain/entities/daily_route_assignment.dart';

class DailyRouteAssignmentModel extends DailyRouteAssignment {
  const DailyRouteAssignmentModel({
    required super.id,
    required super.userId,
    required super.userName,
    required super.routeId,
    required super.routeName,
    required super.assignedDate,
    required super.isActive,
    required super.createdAt,
  });

  factory DailyRouteAssignmentModel.fromJson(Map<String, dynamic> json) =>
      DailyRouteAssignmentModel(
        id: json['id'] as int,
        userId: json['userId'] as int,
        userName: json['userName'] as String,
        routeId: json['routeId'] as int,
        routeName: json['routeName'] as String,
        // assignedDate is DateOnly from the API: "YYYY-MM-DD"
        assignedDate: DateTime.parse(json['assignedDate'] as String),
        isActive: json['isActive'] as bool,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}
