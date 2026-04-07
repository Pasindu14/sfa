import 'package:uswatte/features/route_assignment/domain/entities/daily_route_assignment.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_route.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';

abstract class RouteAssignmentRepository {
  Future<List<RepSummary>> getMyReps();
  Future<List<RepRoute>> getRepRoutes(int userId);
  Future<void> createAssignment({
    required int userId,
    required int routeId,
    required DateTime assignedDate,
  });
  Future<AssignmentsResult> getAssignments({
    int page = 1,
    int pageSize = 50,
    int? userId,
    DateTime? date,
  });
  Future<DailyRouteAssignment?> deleteAssignment(int id, {String? reason});
}
