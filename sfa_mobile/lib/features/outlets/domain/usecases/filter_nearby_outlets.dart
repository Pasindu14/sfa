import 'package:geolocator/geolocator.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';

typedef NearbyOutlet = ({Outlet outlet, double meters});

/// Returns outlets within [radiusMeters] of the rep's position,
/// sorted nearest-first. Outlets whose stored coordinate is (0, 0)
/// are treated as "location unknown" and always included (unfiltered).
class FilterNearbyOutlets {
  const FilterNearbyOutlets();

  List<NearbyOutlet> call({
    required double repLat,
    required double repLng,
    required List<Outlet> outlets,
    required double radiusMeters,
  }) {
    final result = <NearbyOutlet>[];

    for (final outlet in outlets) {
      final noCoord = outlet.latitude == 0.0 && outlet.longitude == 0.0;
      if (noCoord) {
        result.add((outlet: outlet, meters: double.infinity));
        continue;
      }

      final meters = Geolocator.distanceBetween(
        repLat,
        repLng,
        outlet.latitude,
        outlet.longitude,
      );

      if (meters <= radiusMeters) {
        result.add((outlet: outlet, meters: meters));
      }
    }

    result.sort((a, b) => a.meters.compareTo(b.meters));
    return result;
  }
}
