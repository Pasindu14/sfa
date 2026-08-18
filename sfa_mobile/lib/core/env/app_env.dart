class AppEnv {
  AppEnv._();

  /// Pass at build time via --dart-define=SFA_API_DOMAIN=https://your-api.com
  ///
  /// The default is the LIVE production API on purpose. `String.fromEnvironment`
  /// resolves at compile time, so a forgotten --dart-define is not an error — it
  /// silently bakes the default into the binary. Defaulting to production means
  /// that mistake degrades to "local dev is annoying" instead of "every field
  /// device times out", which is what happened when Shorebird patch #3 shipped
  /// the emulator loopback on 2026-08-17.
  ///
  /// For local development against the API running on your host machine, pass:
  ///   --dart-define=SFA_API_DOMAIN=https://10.0.2.2:7169
  /// (On the Android emulator 10.0.2.2 maps to the host's 127.0.0.1; port 7169
  ///  matches sfa_web/.env → SFA_API_DOMAIN=https://127.0.0.1:7169.)
  static const String apiBaseUrl = String.fromEnvironment(
    'SFA_API_DOMAIN',
    defaultValue: 'https://sfa-production-e02a.up.railway.app',
  );
}
