import 'package:uswatte/features/outlet_billings/domain/entities/assigned_route.dart';
import 'package:uswatte/features/outlet_billings/domain/repositories/outlet_billings_repository.dart';

class GetAssignedRoutesUseCase {
  final OutletBillingsRepository _repository;

  const GetAssignedRoutesUseCase(this._repository);

  Future<List<AssignedRoute>> call() => _repository.getAssignedRoutes();
}
