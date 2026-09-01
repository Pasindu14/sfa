import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:uswatte/core/background/background_sync_service.dart';
import 'package:uswatte/core/background/location_tracking_service.dart';
import 'package:uswatte/core/device/device_id_service.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/location/location_gate.dart';
import 'package:uswatte/core/network/session_expired_notifier.dart';
import 'package:uswatte/core/router/app_router.dart';
import 'package:uswatte/core/connectivity/connectivity_service.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/core/update/app_update_service.dart';
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/features/stock/domain/usecases/sync_distributor_stock_usecase.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/auth/domain/usecases/get_current_auth_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/login_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/logout_usecase.dart';
import 'package:uswatte/features/auth/domain/entities/user_role.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:uswatte/core/notifications/fcm_service.dart';
import 'package:uswatte/firebase_options.dart';
import 'package:workmanager/workmanager.dart';

const _backgroundSyncTask = 'backgroundSyncTask';
const _trackingWatchdogTask = 'trackingWatchdogTask';

// @pragma prevents the Dart tree-shaker from removing this function in release
// builds. Without it, WorkManager fires but the callback silently does nothing.
@pragma('vm:entry-point')
void callbackDispatcher() {
  Workmanager().executeTask((taskName, inputData) async {
    try {
      // Background isolate has no platform binding — must initialize before
      // calling any Flutter plugin (sqflite, flutter_secure_storage, etc.).
      WidgetsFlutterBinding.ensureInitialized();
      await configureDependencies();

      // Branch on the task: the dispatcher is shared by every registered task,
      // so without this the watchdog would run a full data sync every 15 minutes.
      if (taskName == _trackingWatchdogTask) {
        await LocationTrackingService.reviveIfEnabled();
      } else {
        await getIt<BackgroundSyncService>().runSync();
      }
    } catch (_) {
      // Never surface exceptions to WorkManager — it retries immediately on
      // failure, which would drain battery and spam the server.
    }
    return Future.value(true);
  });
}

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);
  await configureDependencies();

  // Create/migrate the SQLite schema here, on the UI isolate, BEFORE any
  // background isolate can open the same file. Two isolates opening it
  // concurrently makes sqflite force a ROLLBACK on the shared native
  // connection, aborting the schema transaction half-way. See DatabaseHelper.
  await DatabaseHelper.instance.database;

  // Register the 4-hour background sync task. ExistingWorkPolicy.keep means
  // relaunching the app does not reset the timer for an already-queued task.
  await LocationTrackingService.initialize();
  await Workmanager().initialize(callbackDispatcher);
  await Workmanager().registerPeriodicTask(
    'com.sfa.uswatte.background_sync',
    _backgroundSyncTask,
    frequency: const Duration(hours: 4),
    constraints: Constraints(networkType: NetworkType.connected),
    existingWorkPolicy: ExistingPeriodicWorkPolicy.keep,
  );

  // Watchdog: restart location tracking if it should be running but isn't.
  // WorkManager survives the process death that kills the foreground service, and
  // 15 minutes is its minimum period. No network constraint — starting a service
  // needs none, and requiring connectivity would leave a dead service dead through
  // exactly the offline stretch we still want positions recorded for.
  await Workmanager().registerPeriodicTask(
    'com.sfa.uswatte.tracking_watchdog',
    _trackingWatchdogTask,
    frequency: const Duration(minutes: 15),
    existingWorkPolicy: ExistingPeriodicWorkPolicy.keep,
  );

  // Composition root: wire use cases explicitly — presentation never touches getIt
  final authBloc = AuthBloc(
    loginUseCase: getIt<LoginUseCase>(),
    logoutUseCase: getIt<LogoutUseCase>(),
    getCurrentAuthUseCase: getIt<GetCurrentAuthUseCase>(),
    deviceIdService: getIt<DeviceIdService>(),
    fcmService: getIt<FcmService>(),
  )..add(const AppStarted());

  runApp(SfaApp(authBloc: authBloc));
}

class SfaApp extends StatefulWidget {
  final AuthBloc authBloc;

  const SfaApp({super.key, required this.authBloc});

  @override
  State<SfaApp> createState() => _SfaAppState();
}

class _SfaAppState extends State<SfaApp> with WidgetsBindingObserver {
  StreamSubscription<void>? _sessionExpiredSub;
  StreamSubscription<AuthState>? _authSyncSub;
  // Guards against a second post-login sync starting while the first is still
  // running (e.g. AuthAuthenticated re-emitted by a token refresh).
  bool _postLoginSyncing = false;
  StreamSubscription<void>? _connectivityStockSub;
  StreamSubscription<RemoteMessage>? _fcmForegroundSub;
  StreamSubscription<RemoteMessage>? _fcmTapSub;
  // Built once. Recreating GoRouter on every build (e.g. inside MaterialApp.router)
  // tears down its listenables mid-frame and produces "markNeedsBuild during build"
  // errors during route transitions.
  late final GoRouter _router;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _router = AppRouter.createRouter(widget.authBloc);
    // SessionExpired, not LogoutRequested — a failed refresh must send the rep
    // to the login screen without also killing location tracking and binning
    // their queued pings for the rest of the route.
    _sessionExpiredSub = getIt<SessionExpiredNotifier>().stream.listen((_) {
      widget.authBloc.add(const SessionExpired());
    });
    // SFA-119: pull fresh master data the moment a session becomes active —
    // a real login, or a token restore on cold start. Without this the rep
    // works off whatever was cached last session until they open Sync
    // manually, which is how stale stock counts reach the bill screen.
    _authSyncSub = widget.authBloc.stream.listen(_onAuthStateChanged);
    if (widget.authBloc.state is AuthAuthenticated) {
      _onAuthStateChanged(widget.authBloc.state);
    }
    // Sync distributor stock whenever connectivity is restored (fire-and-forget).
    _connectivityStockSub = getIt<ConnectivityService>()
        .onConnectionRestored
        .listen((_) => unawaited(
              getIt<SyncDistributorStockUseCase>()().catchError((_) {}),
            ));
    _setupNotificationHandlers();
    // Staggered so the check doesn't compete with launch work (schema open,
    // auth restore, first sync).
    Future.delayed(const Duration(seconds: 8), _checkForPatch);
  }

  // ── Post-login sync ─────────────────────────────────────────────────────────

  /// Runs a full sync when a session becomes active. Only sales reps get it:
  /// products, outlets, the daily route assignment and distributor stock are
  /// all rep-scoped data, and the supervisor/distributor screens fetch live
  /// from the API instead of the local cache.
  void _onAuthStateChanged(AuthState state) {
    if (state is! AuthAuthenticated) {
      // A logout ends the guard too, so the next login syncs again.
      _postLoginSyncing = false;
      return;
    }
    if (state.role != UserRole.salesRep) return;
    if (_postLoginSyncing) return;
    _postLoginSyncing = true;
    unawaited(_runPostLoginSync());
  }

  Future<void> _runPostLoginSync() async {
    try {
      if (!await getIt<ConnectivityService>().hasInternet()) return;
      // Every step inside runSync is individually guarded.
      await getIt<BackgroundSyncService>().runSync();
    } catch (_) {
      // Best-effort: the rep can still work offline off the existing cache,
      // and Sync / connectivity-restored / app-resume all retry later.
    } finally {
      _postLoginSyncing = false;
    }
  }

  // ── Shorebird patch prompt + auto-apply ─────────────────────────────────────

  /// How long the app must stay backgrounded before a staged patch is applied
  /// by killing the process. Long enough that a phone call or a quick hop to
  /// another app doesn't discard a half-entered bill form, short enough that
  /// the patch lands the same working day.
  static const _backgroundRestartDelay = Duration(minutes: 2);

  bool _updateBannerVisible = false;
  bool _patchStaged = false;
  Timer? _autoRestartTimer;

  /// Applies a staged patch once the rep has been out of the app long enough
  /// to be done with it.
  ///
  /// Killing the process also kills the location foreground service, but
  /// flutter_background_service arms an AlarmManager watchdog 5s out
  /// (WatchdogReceiver) that respawns it — and the respawned isolate boots the
  /// patched code. Tracking resumes on its own; it is not left off.
  void _scheduleAutoRestart() {
    if (!_patchStaged) return;
    _autoRestartTimer?.cancel();
    _autoRestartTimer = Timer(
      _backgroundRestartDelay,
      () => getIt<AppUpdateService>().restart(),
    );
  }

  /// Downloads any pending patch and, if one is staged, offers a restart.
  ///
  /// Without this the rep has no way to know a patch is waiting: Shorebird only
  /// applies patches at process start, and the location foreground service keeps
  /// this app's process alive across swipe-away. See [AppUpdateService].
  Future<void> _checkForPatch() async {
    final needsRestart = await getIt<AppUpdateService>().checkAndDownload();
    _patchStaged = needsRestart;
    if (!needsRestart || !mounted || _updateBannerVisible) return;

    final ctx = _router.routerDelegate.navigatorKey.currentContext;
    if (ctx == null) return;
    // ignore: use_build_context_synchronously
    final messenger = ScaffoldMessenger.maybeOf(ctx);
    if (messenger == null) return;

    _updateBannerVisible = true;
    messenger.showMaterialBanner(
      MaterialBanner(
        content: const Text(
          'An update is ready. It applies automatically once you leave the '
          'app, or restart now.',
        ),
        leading: const Icon(Icons.system_update_rounded),
        actions: [
          TextButton(
            onPressed: () {
              messenger.hideCurrentMaterialBanner();
              _updateBannerVisible = false;
            },
            child: const Text('LATER'),
          ),
          TextButton(
            onPressed: () => getIt<AppUpdateService>().restart(),
            child: const Text('RESTART NOW'),
          ),
        ],
      ),
    );
  }

  void _setupNotificationHandlers() {
    // App was fully terminated and opened by tapping a notification
    FirebaseMessaging.instance.getInitialMessage().then((message) {
      if (message != null) _navigateFromNotification(message.data);
    });

    // App was in background and notification was tapped
    _fcmTapSub = FirebaseMessaging.onMessageOpenedApp.listen((message) {
      _navigateFromNotification(message.data);
    });

    // App is in foreground — show an in-app banner
    _fcmForegroundSub = FirebaseMessaging.onMessage.listen((message) {
      if (!mounted) return;
      final notification = message.notification;
      if (notification == null) return;
      final ctx = _router.routerDelegate.navigatorKey.currentContext;
      if (ctx == null) return;
      // ignore: use_build_context_synchronously
      ScaffoldMessenger.maybeOf(ctx)?.showSnackBar(
        SnackBar(
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                notification.title ?? '',
                style: const TextStyle(fontWeight: FontWeight.bold),
              ),
              if (notification.body != null) Text(notification.body!),
            ],
          ),
          duration: const Duration(seconds: 5),
          action: SnackBarAction(
            label: 'View',
            onPressed: () => _navigateFromNotification(message.data),
          ),
        ),
      );
    });
  }

  void _navigateFromNotification(Map<String, dynamic> data) {
    final type = data['type'] as String?;
    if (type == null) return;
    // Navigate to bills list so the rep can see the updated status.
    // The router guards will ensure the user is authenticated before proceeding.
    if (type == 'BILL_APPROVED' || type == 'BILL_REJECTED') {
      // Push so the rep's existing stack (home) stays underneath and back
      // returns there. `goNamed` REPLACES the stack, which would leave the
      // bills list with nothing below it. On a cold start opened straight
      // from a notification there is no stack yet — we're still on splash
      // while auth resolves — so fall back to `go`, which the auth redirect
      // holds until authentication completes.
      final location = _router.routerDelegate.currentConfiguration.uri.path;
      if (location.startsWith('/sales-rep')) {
        _router.pushNamed('bills');
      } else {
        _router.goNamed('bills');
      }
    }
  }

  @override
  void dispose() {
    _sessionExpiredSub?.cancel();
    _authSyncSub?.cancel();
    _connectivityStockSub?.cancel();
    _fcmForegroundSub?.cancel();
    _fcmTapSub?.cancel();
    _autoRestartTimer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  /// When the app returns to the foreground, flush any pending bills. The
  /// connectivity listener already handles the offline → online edge, but this
  /// catches the case where the rep left the app backgrounded long enough that
  /// the OS suspended our network callbacks. `flushAll` also runs the 14-day
  /// retention purge at the end, so synced bills don't accumulate indefinitely.
  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      // The rep came back — they're not done with the app, so don't apply a
      // staged patch out from under them.
      _autoRestartTimer?.cancel();
      _autoRestartTimer = null;
      // Fire-and-forget; errors are contained inside the service.
      getIt<BillSyncService>().flushAll();
      unawaited(getIt<SyncDistributorStockUseCase>()().catchError((_) {}));
      unawaited(_checkForPatch());
    } else if (state == AppLifecycleState.paused) {
      _scheduleAutoRestart();
    }
  }

  @override
  Widget build(BuildContext context) {
    return ScreenUtilInit(
      // Design baseline: 390×844 (iPhone 14 logical pixels)
      designSize: const Size(390, 844),
      minTextAdapt: true,
      splitScreenMode: true,
      builder: (context, child) {
        return MultiBlocProvider(
          providers: [
            BlocProvider<AuthBloc>.value(value: widget.authBloc),
          ],
          child: AnnotatedRegion<SystemUiOverlayStyle>(
            value: const SystemUiOverlayStyle(
              statusBarColor: Colors.transparent,
              statusBarIconBrightness: Brightness.dark,
              statusBarBrightness: Brightness.light,
              systemNavigationBarColor: Colors.transparent,
              systemNavigationBarIconBrightness: Brightness.dark,
            ),
            child: MaterialApp.router(
              title: 'SFA Uswatte',
              debugShowCheckedModeBanner: false,
              theme: AppTheme.light,
              routerConfig: _router,
              // Wraps whatever the router renders, so a sales rep with location off is
              // blocked on every screen rather than per-page. Deliberately here and not
              // as a builder on the /sales-rep GoRoute — that pushes a phantom blank
              // page under every screen and breaks the back button.
              builder: (context, child) =>
                  LocationGate(child: child ?? const SizedBox.shrink()),
            ),
          ),
        );
      },
    );
  }
}
