import 'package:uswatte/features/route_assignment/domain/repositories/route_assignment_repository.dart';

class CreateAssignmentUseCase {
  final RouteAssignmentRepository _repo;
  const CreateAssignmentUseCase(this._repo);

  Future<void> call({
    required int userId,
    required int routeId,
    required DateTime assignedDate,
  }) =>
      _repo.createAssignment(
        userId: userId,
        routeId: routeId,
        assignedDate: assignedDate,
      );
}
