import 'package:uswatte/features/outlet_billings/domain/entities/assigned_route.dart';

class RouteAssignmentModel {
  final int routeId;
  final String routeName;
  final String assignedDate;

  const RouteAssignmentModel({
    required this.routeId,
    required this.routeName,
    required this.assignedDate,
  });

  factory RouteAssignmentModel.fromJson(Map<String, dynamic> json) =>
      RouteAssignmentModel(
        routeId: json['routeId'] as int,
        routeName: json['routeName'] as String,
        assignedDate: json['assignedDate'] as String,
      );

  AssignedRoute toEntity() => AssignedRoute(
        routeId: routeId,
        routeName: routeName,
        assignedDate: parsedDate,
      );

  DateTime get parsedDate => DateTime.parse(assignedDate);
}
