import 'dart:math';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:uswatte/core/constants/app_constants.dart';

/// Provides a stable, unique identifier for this app installation.
///
/// Generated once as a UUID v4 on first call, then persisted in
/// [FlutterSecureStorage]. Stable across app updates and restarts.
/// Resets only on app uninstall (Android) or full app data clear (iOS).
///
/// This is the industry-standard approach for TOFU device binding —
/// the same strategy used by Firebase, Amplitude, and most analytics SDKs.
class DeviceIdService {
  final FlutterSecureStorage _storage;

  const DeviceIdService(this._storage);

  Future<String> getDeviceId() async {
    final stored = await _storage.read(key: AppConstants.deviceIdKey);
    if (stored != null) return stored;

    final id = _generateUuidV4();
    await _storage.write(key: AppConstants.deviceIdKey, value: id);
    return id;
  }

  /// Generates a RFC 4122 UUID v4 (random).
  static String _generateUuidV4() {
    final rng = Random.secure();
    final bytes = List<int>.generate(16, (_) => rng.nextInt(256));

    // Set version bits (version 4)
    bytes[6] = (bytes[6] & 0x0f) | 0x40;
    // Set variant bits (variant 1)
    bytes[8] = (bytes[8] & 0x3f) | 0x80;

    String hex(int byte) => byte.toRadixString(16).padLeft(2, '0');

    return '${hex(bytes[0])}${hex(bytes[1])}${hex(bytes[2])}${hex(bytes[3])}-'
        '${hex(bytes[4])}${hex(bytes[5])}-'
        '${hex(bytes[6])}${hex(bytes[7])}-'
        '${hex(bytes[8])}${hex(bytes[9])}-'
        '${hex(bytes[10])}${hex(bytes[11])}${hex(bytes[12])}${hex(bytes[13])}${hex(bytes[14])}${hex(bytes[15])}';
  }
}
