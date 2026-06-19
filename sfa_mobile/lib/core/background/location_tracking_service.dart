import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:geolocator/geolocator.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/core/di/injection.dart';

const _channelId = 'sfa_location_tracking';
const _channelName = 'SFA Location Tracking';
const _notificationId = 888;
const _maxAccuracyMetres = 100.0;

// ── Background isolate entry points ────────────────────────────────────────
// Must be top-level functions with @pragma so the VM keeps them in release builds.

@pragma('vm:entry-point')
void locationServiceEntry(ServiceInstance service) async {
  WidgetsFlutterBinding.ensureInitialized();
  await configureDependencies();

  // Capture + flush immediately on start, then repeat every 5 minutes.
  await _tick();
  final timer = Timer.periodic(const Duration(minutes: 5), (_) async {
    await _tick();
  });

  service.on('stop').listen((_) {
    timer.cancel();
    service.stopSelf();
  });
}

@pragma('vm:entry-point')
Future<bool> locationServiceIosBackground(ServiceInstance service) async {
  WidgetsFlutterBinding.ensureInitialized();
  return true;
}

// ── Private helpers (run inside background isolate) ─────────────────────────

Future<void> _tick() async {
  try {
    await _captureAndQueue();
  } catch (_) {}
  try {
    await _flushQueue();
  } catch (_) {}
}

Future<void> _captureAndQueue() async {
  final permission = await Geolocator.checkPermission();
  if (permission == LocationPermission.denied ||
      permission == LocationPermission.deniedForever) {
    return;
  }
  if (!await Geolocator.isLocationServiceEnabled()) {
    return;
  }

  final position = await Geolocator.getCurrentPosition(
    locationSettings: const LocationSettings(
      accuracy: LocationAccuracy.medium,
      timeLimit: Duration(seconds: 10),
    ),
  );

  if (position.latitude == 0.0 && position.longitude == 0.0) {
    return;
  }
  if (position.accuracy > _maxAccuracyMetres) {
    return;
  }

  final database = await DatabaseHelper.instance.database;
  await database.insert('pending_location_pings', {
    'lat': position.latitude,
    'lng': position.longitude,
    'accuracy': position.accuracy,
    'recorded_at': position.timestamp.toUtc().toIso8601String(),
    'created_at': DateTime.now().toUtc().toIso8601String(),
  });
}

Future<void> _flushQueue() async {
  final database = await DatabaseHelper.instance.database;
  final rows =
      await database.query('pending_location_pings', orderBy: 'id ASC');
  if (rows.isEmpty) {
    return;
  }

  final payload = rows
      .map((r) => {
            'latitude': r['lat'],
            'longitude': r['lng'],
            'accuracy': r['accuracy'],
            'recordedAt': r['recorded_at'],
          })
      .toList();

  final dio = getIt<Dio>();
  await dio.post('/api/v1/location-pings', data: {'pings': payload});

  // Only delete rows that were successfully uploaded.
  final ids = rows.map((r) => r['id'] as int).toList();
  final placeholders = ids.map((_) => '?').join(',');
  await database.rawDelete(
    'DELETE FROM pending_location_pings WHERE id IN ($placeholders)',
    ids,
  );
}

/// Public flush entrypoint — called by BackgroundSyncService as a backstop.
Future<void> flushLocationPingQueue() => _flushQueue();

// ── Public API ──────────────────────────────────────────────────────────────

class LocationTrackingService {
  static final _service = FlutterBackgroundService();

  /// Call once from main() after Firebase init, before the app widget is built.
  /// Creates the Android notification channel required before a foreground
  /// service can post its persistent notification (Android 8+ requirement).
  static Future<void> initialize() async {
    const channel = AndroidNotificationChannel(
      _channelId,
      _channelName,
      description: 'Shows while your location is being tracked for field ops.',
      importance: Importance.low,
      playSound: false,
    );
    await FlutterLocalNotificationsPlugin()
        .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(channel);

    await _service.configure(
      androidConfiguration: AndroidConfiguration(
        onStart: locationServiceEntry,
        autoStart: false,
        isForegroundMode: true,
        notificationChannelId: _channelId,
        initialNotificationTitle: 'SFA',
        initialNotificationContent: 'Location tracking active',
        foregroundServiceNotificationId: _notificationId,
      ),
      iosConfiguration: IosConfiguration(
        autoStart: false,
        onForeground: locationServiceEntry,
        onBackground: locationServiceIosBackground,
      ),
    );
  }

  static Future<void> start() async {
    final isRunning = await _service.isRunning();
    if (!isRunning) {
      await _service.startService();
    }
  }

  static Future<void> stop() async {
    _service.invoke('stop');
    // Clear the outbox so stale pings don't upload after next login.
    final database = await DatabaseHelper.instance.database;
    await database.delete('pending_location_pings');
  }
}
