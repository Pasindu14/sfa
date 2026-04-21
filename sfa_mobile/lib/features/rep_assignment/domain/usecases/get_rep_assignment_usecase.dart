import 'package:uswatte/features/rep_assignment/domain/entities/rep_assignment.dart';
import 'package:uswatte/features/rep_assignment/domain/repositories/rep_assignment_repository.dart';

class GetRepAssignmentUseCase {
  final RepAssignmentRepository _repository;
  const GetRepAssignmentUseCase(this._repository);

  Future<RepAssignment> call() => _repository.getMyAssignment();
}
