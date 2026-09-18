import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';
import 'package:uswatte/features/pricing/domain/repositories/pricing_repository.dart';

class GetPricingStructuresUseCase {
  final PricingRepository _repository;
  const GetPricingStructuresUseCase(this._repository);

  Future<List<PricingStructure>> call() => _repository.getPricingStructures();
}
