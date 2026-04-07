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
}
