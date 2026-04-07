import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/route_assignment/domain/repositories/route_assignment_repository.dart';

class GetMyRepsUseCase {
  final RouteAssignmentRepository _repo;
  const GetMyRepsUseCase(this._repo);

  Future<List<RepSummary>> call() => _repo.getMyReps();
}
