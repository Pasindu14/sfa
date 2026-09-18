import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';

abstract interface class PricingRepository {
  /// The locally cached structures, default first. Empty if never synced.
  Future<List<PricingStructure>> getPricingStructures();

  /// Downloads every active structure with its prices and replaces the local
  /// copy. Sends the stored ETag (when there is local data) so an unchanged
  /// set costs a 304; [force] skips that and always downloads.
  Future<(List<PricingStructure>, DateTime)> syncPricingStructures({
    bool force = false,
  });

  /// The timestamp of the last successful sync, or null if never synced.
  Future<DateTime?> getLastSyncedAt();
}
