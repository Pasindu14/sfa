import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';
import 'package:uswatte/features/pricing/domain/repositories/pricing_repository.dart';

class GetPricingUseCase {
  final PricingRepository _repository;

  const GetPricingUseCase(this._repository);

  Future<List<PricingStructure>> call() => _repository.getPricingStructures();
}
