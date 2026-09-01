class AppConstants {
  AppConstants._();

  static const String appName = 'SFA Uswatte';
  static const String apiBaseUrlKey = 'SFA_API_DOMAIN';
  static const String accessTokenKey = 'access_token';
  static const String refreshTokenKey = 'refresh_token';
  static const String deviceIdKey = 'device_id';

  /// Whether background location tracking should be running.
  ///
  /// Deliberately separate from the auth token. A token can vanish on a failed
  /// refresh while the rep is still mid-route, and that must not stop tracking —
  /// only a deliberate logout clears this flag. It is also what the background
  /// isolate checks on a boot auto-start, so the service does not run for a
  /// device where nobody is logged in.
  static const String trackingEnabledKey = 'tracking_enabled';

  /// Offline fallback for the billing proximity gate.
  /// The live value is pushed from the server via the daily outlet sync
  /// and stored in SQLite — this constant is only used before the first sync.
  static const double billingProximityRadiusMeters = 1000.0;
}
