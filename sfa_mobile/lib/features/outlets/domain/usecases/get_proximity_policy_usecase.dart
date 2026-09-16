import 'package:uswatte/features/outlets/domain/entities/proximity_policy.dart';
import 'package:uswatte/features/outlets/domain/repositories/outlets_repository.dart';

class GetProximityPolicyUseCase {
  final OutletsRepository _repository;
  const GetProximityPolicyUseCase(this._repository);

  Future<ProximityPolicy?> call() => _repository.getProximityPolicy();
}
