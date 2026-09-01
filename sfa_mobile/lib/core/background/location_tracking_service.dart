import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:geolocator/geolocator.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/core/di/injection.dart';

const _channelId = 'sfa_location_tracking';
const _channelName = 'SFA Location Tracking';
const _notificationId = 888;
const _maxAccuracyMetres = 100.0;

/// Upper bound on the offline outbox — roughly a week at one ping per 5 minutes.
///
/// Uploads only delete rows on success, so a rep whose session expired and who
/// never logs back in would otherwise queue rows forever. Trimming the oldest
/// keeps the most recent (most useful) positions and bounds the DB.
const _maxQueuedPings = 2000;

/// Delay before this isolate first touches SQLite. The UI isolate creates and
/// migrates the schema on launch, and this isolate opening the same file
/// mid-transaction makes sqflite force a ROLLBACK on the shared native
/// connection — leaving a half-built schema. See [DatabaseHelper].
const _firstTickDelay = Duration(seconds: 20);

// ── Background isolate entry points ────────────────────────────────────────
// Must be top-level functions with @pragma so the VM keeps them in release builds.

@pragma('vm:entry-point')
void locationServiceEntry(ServiceInstance service) async {
  WidgetsFlutterBinding.ensureInitialized();
  await configureDependencies();

  // Listen for stop before the stagger below, so a stop arriving during the
  // delay is still honoured.
  Timer? timer;
  var stopped = false;
  service.on('stop').listen((_) {
    stopped = true;
    timer?.cancel();
    service.stopSelf();
  });

  // The plugin's autoStartOnBoot defaults to true, so this isolate also runs
  // after a device restart with no app launch behind it. If nobody is logged
  // in, shut down instead of capturing positions that can never be uploaded.
  if (!await LocationTrackingService.isEnabled()) {
    service.stopSelf();
    return;
  }

  // Stagger the first DB access past app startup — see [_firstTickDelay].
  await Future<void>.delayed(_firstTickDelay);
  if (stopped) return;

  // Capture + flush, then repeat every 5 minutes.
  await _tick();
  timer = Timer.periodic(const Duration(minutes: 5), (_) async {
    await _tick();
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

  // Drop the oldest rows once the outbox exceeds its cap. Only reached when
  // uploads have been failing for days — the newest positions are the ones
  // worth keeping.
  await database.rawDelete(
    '''
    DELETE FROM pending_location_pings
    WHERE id NOT IN (
      SELECT id FROM pending_location_pings ORDER BY id DESC LIMIT ?
    )
    ''',
    [_maxQueuedPings],
  );
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

  /// Whether tracking is meant to be running. Survives token loss — only
  /// [stop] clears it. Read by the background isolate on a boot auto-start.
  static Future<bool> isEnabled() async {
    try {
      final value = await getIt<FlutterSecureStorage>()
          .read(key: AppConstants.trackingEnabledKey);
      return value == '1';
    } catch (_) {
      // Storage unavailable in this isolate — assume off rather than tracking
      // a device we cannot prove is logged in.
      return false;
    }
  }

  /// Marks tracking as wanted and starts the service if it isn't already up.
  /// Safe to call repeatedly — used on login and on every session restore, so
  /// reopening the app also revives a service the OS killed.
  static Future<void> start() async {
    try {
      await getIt<FlutterSecureStorage>()
          .write(key: AppConstants.trackingEnabledKey, value: '1');
    } catch (_) {
      // Fall through — a running service is still better than none.
    }

    final isRunning = await _service.isRunning();
    if (!isRunning) {
      await _service.startService();
    }
  }

  /// Deliberate logout only. Clears the flag so a boot auto-start won't revive
  /// tracking, and empties the outbox so a previous rep's positions are not
  /// uploaded under whoever logs in next.
  ///
  /// Do NOT call this on session expiry — a failed token refresh does not mean
  /// the rep stopped working, and dropping the queue there loses real data.
  static Future<void> stop() async {
    try {
      await getIt<FlutterSecureStorage>()
          .delete(key: AppConstants.trackingEnabledKey);
    } catch (_) {
      // Ignore — the stopSelf below is what actually halts capture.
    }

    _service.invoke('stop');
    final database = await DatabaseHelper.instance.database;
    await database.delete('pending_location_pings');
  }
}
