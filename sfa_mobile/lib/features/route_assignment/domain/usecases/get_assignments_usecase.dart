import 'package:uswatte/features/route_assignment/domain/entities/daily_route_assignment.dart';
import 'package:uswatte/features/route_assignment/domain/repositories/route_assignment_repository.dart';

class GetAssignmentsUseCase {
  final RouteAssignmentRepository _repo;
  const GetAssignmentsUseCase(this._repo);

  Future<AssignmentsResult> call({
    int page = 1,
    int pageSize = 50,
    int? userId,
    DateTime? date,
  }) =>
      _repo.getAssignments(
        page: page,
        pageSize: pageSize,
        userId: userId,
        date: date,
      );
}
