import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/outlets/domain/repositories/outlets_repository.dart';

class GetOutletsUseCase {
  final OutletsRepository _repository;
  const GetOutletsUseCase(this._repository);

  Future<List<Outlet>> call() => _repository.getOutlets();
}
