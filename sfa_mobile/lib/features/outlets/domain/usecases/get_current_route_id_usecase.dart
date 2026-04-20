import 'package:uswatte/features/outlets/domain/repositories/outlets_repository.dart';

class GetCurrentRouteIdUseCase {
  final OutletsRepository _repository;
  const GetCurrentRouteIdUseCase(this._repository);

  Future<int?> call() => _repository.getCurrentRouteId();
}
