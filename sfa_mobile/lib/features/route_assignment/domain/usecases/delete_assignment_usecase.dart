import 'package:uswatte/features/route_assignment/domain/entities/daily_route_assignment.dart';
import 'package:uswatte/features/route_assignment/domain/repositories/route_assignment_repository.dart';

class DeleteAssignmentUseCase {
  final RouteAssignmentRepository _repo;
  const DeleteAssignmentUseCase(this._repo);

  /// Returns the updated assignment (pending approval) or null (direct delete).
  Future<DailyRouteAssignment?> call(int id, {String? reason}) =>
      _repo.deleteAssignment(id, reason: reason);
}
