import 'package:equatable/equatable.dart';

/// A rep's single most recent location ping
/// (`GET /api/v1/supervisor/rep-last-location`).
class RepLastLocation extends Equatable {
  final double latitude;
  final double longitude;
  final double accuracyMeters;

  /// When the phone captured the fix (local time).
  final DateTime recordedAt;

  const RepLastLocation({
    required this.latitude,
    required this.longitude,
    required this.accuracyMeters,
    required this.recordedAt,
  });

  factory RepLastLocation.fromJson(Map<String, dynamic> json) => RepLastLocation(
        latitude: (json['latitude'] as num).toDouble(),
        longitude: (json['longitude'] as num).toDouble(),
        accuracyMeters: (json['accuracy'] as num?)?.toDouble() ?? 0,
        recordedAt: DateTime.parse(json['recordedAt'] as String).toLocal(),
      );

  /// Pings upload in batches every few minutes; beyond this the dot is no
  /// longer "where the rep is", only "where the rep was".
  static const staleAfter = Duration(minutes: 30);

  bool isStale(DateTime now) => now.difference(recordedAt) > staleAfter;

  @override
  List<Object?> get props => [latitude, longitude, accuracyMeters, recordedAt];
}
