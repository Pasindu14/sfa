import 'package:uswatte/features/route_assignment/domain/repositories/route_assignment_repository.dart';

class DeleteAssignmentUseCase {
  final RouteAssignmentRepository _repo;
  const DeleteAssignmentUseCase(this._repo);

  Future<void> call(int id) => _repo.deleteAssignment(id);
}
