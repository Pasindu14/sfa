import 'package:equatable/equatable.dart';

class AssignedRoute extends Equatable {
  final int routeId;
  final String routeName;
  final DateTime assignedDate;

  const AssignedRoute({
    required this.routeId,
    required this.routeName,
    required this.assignedDate,
  });

  @override
  List<Object?> get props => [routeId, routeName, assignedDate];
}
