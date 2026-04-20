import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';

abstract interface class PricingRepository {
  Future<List<PricingStructure>> getPricingStructures();
  Future<(List<PricingStructure>, DateTime)> syncPricingStructures();
  Future<DateTime?> getLastSyncedAt();
}
