class AppEnv {
  AppEnv._();

  /// Pass at build time via --dart-define=SFA_API_DOMAIN=https://your-api.com
  /// Android emulator: 10.0.2.2 maps to the host machine's 127.0.0.1.
  /// Port 7169 matches sfa_web/.env → SFA_API_DOMAIN=https://127.0.0.1:7169
  ///     defaultValue: 'https://sfa-production-e02a.up.railway.app',
  static const String apiBaseUrl = String.fromEnvironment(
    'SFA_API_DOMAIN',
    defaultValue: 'https://sfa-production-e02a.up.railway.app'
  );
}
