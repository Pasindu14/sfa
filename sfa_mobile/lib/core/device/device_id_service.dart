import 'dart:math';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:uswatte/core/constants/app_constants.dart';

/// Provides a stable, unique device identifier.
/// Generated once on first launch and persisted in secure storage.
/// Does not depend on hardware info — survives OS updates and resets.
class DeviceIdService {
  final FlutterSecureStorage _storage;

  const DeviceIdService(this._storage);

  /// Returns the stored device ID, generating one if it doesn't exist yet.
  Future<String> getOrCreate() async {
    final existing = await _storage.read(key: AppConstants.deviceIdKey);
    if (existing != null) return existing;

    final id = _generateUuidV4();
    await _storage.write(key: AppConstants.deviceIdKey, value: id);
    return id;
  }

  /// RFC 4122 UUID v4 using cryptographically secure random bytes.
  static String _generateUuidV4() {
    final rng = Random.secure();
    final bytes = List<int>.generate(16, (_) => rng.nextInt(256));
    // Set version bits to 4 (0100xxxx)
    bytes[6] = (bytes[6] & 0x0f) | 0x40;
    // Set variant bits to RFC 4122 (10xxxxxx)
    bytes[8] = (bytes[8] & 0x3f) | 0x80;
    final hex = bytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join();
    return '${hex.substring(0, 8)}-${hex.substring(8, 12)}-'
        '${hex.substring(12, 16)}-${hex.substring(16, 20)}-${hex.substring(20)}';
  }
}
