import 'package:uswatte/features/route_assignment/data/datasources/route_assignment_remote_datasource.dart';
import 'package:uswatte/features/route_assignment/domain/entities/daily_route_assignment.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_route.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/route_assignment/domain/repositories/route_assignment_repository.dart';

class RouteAssignmentRepositoryImpl implements RouteAssignmentRepository {
  final RouteAssignmentRemoteDatasource _datasource;
  const RouteAssignmentRepositoryImpl(this._datasource);

  @override
  Future<List<RepSummary>> getMyReps() => _datasource.getMyReps();

  @override
  Future<List<RepRoute>> getRepRoutes(int userId) =>
      _datasource.getRepRoutes(userId);

  @override
  Future<void> createAssignment({
    required int userId,
    required int routeId,
    required DateTime assignedDate,
  }) =>
      _datasource.createAssignment(
        userId: userId,
        routeId: routeId,
        assignedDate: assignedDate,
      );

  @override
  Future<AssignmentsResult> getAssignments({
    int page = 1,
    int pageSize = 50,
    int? userId,
    DateTime? date,
  }) =>
      _datasource.getAssignments(
        page: page,
        pageSize: pageSize,
        userId: userId,
        date: date,
      );

  @override
  Future<DailyRouteAssignment?> deleteAssignment(int id, {String? reason}) =>
      _datasource.deleteAssignment(id, reason: reason);
}
