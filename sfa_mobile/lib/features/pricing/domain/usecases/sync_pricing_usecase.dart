import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';
import 'package:uswatte/features/pricing/domain/repositories/pricing_repository.dart';

class SyncPricingUseCase {
  final PricingRepository _repository;

  const SyncPricingUseCase(this._repository);

  Future<(List<PricingStructure>, DateTime)> call() =>
      _repository.syncPricingStructures();
}
