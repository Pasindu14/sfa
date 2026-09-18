import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';
import 'package:uswatte/features/pricing/domain/repositories/pricing_repository.dart';

class SyncPricingStructuresUseCase {
  final PricingRepository _repository;
  const SyncPricingStructuresUseCase(this._repository);

  Future<(List<PricingStructure>, DateTime)> call({bool force = false}) =>
      _repository.syncPricingStructures(force: force);
}
