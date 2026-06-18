class AppConstants {
  AppConstants._();

  static const String appName = 'SFA Uswatte';
  static const String apiBaseUrlKey = 'SFA_API_DOMAIN';
  static const String accessTokenKey = 'access_token';
  static const String refreshTokenKey = 'refresh_token';
  static const String deviceIdKey = 'device_id';

  /// Offline fallback for the billing proximity gate.
  /// The live value is pushed from the server via the daily outlet sync
  /// and stored in SQLite — this constant is only used before the first sync.
  static const double billingProximityRadiusMeters = 1000.0;
}
