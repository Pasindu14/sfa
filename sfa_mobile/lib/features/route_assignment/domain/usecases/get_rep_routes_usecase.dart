import 'package:uswatte/features/route_assignment/domain/entities/rep_route.dart';
import 'package:uswatte/features/route_assignment/domain/repositories/route_assignment_repository.dart';

class GetRepRoutesUseCase {
  final RouteAssignmentRepository _repo;
  const GetRepRoutesUseCase(this._repo);

  Future<List<RepRoute>> call(int userId) => _repo.getRepRoutes(userId);
}
