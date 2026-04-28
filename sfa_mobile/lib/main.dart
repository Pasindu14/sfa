import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:uswatte/core/device/device_id_service.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/network/session_expired_notifier.dart';
import 'package:uswatte/core/router/app_router.dart';
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/auth/domain/usecases/get_current_auth_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/login_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/logout_usecase.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await configureDependencies();

  // Composition root: wire use cases explicitly — presentation never touches getIt
  final authBloc = AuthBloc(
    loginUseCase: getIt<LoginUseCase>(),
    logoutUseCase: getIt<LogoutUseCase>(),
    getCurrentAuthUseCase: getIt<GetCurrentAuthUseCase>(),
    deviceIdService: getIt<DeviceIdService>(),
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

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _sessionExpiredSub = getIt<SessionExpiredNotifier>().stream.listen((_) {
      widget.authBloc.add(LogoutRequested());
    });
  }

  @override
  void dispose() {
    _sessionExpiredSub?.cancel();
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
      // Fire-and-forget; errors are contained inside the service.
      getIt<BillSyncService>().flushAll();
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
          child: MaterialApp.router(
            title: 'SFA Uswatte',
            debugShowCheckedModeBanner: false,
            theme: AppTheme.light,
            routerConfig: AppRouter.createRouter(widget.authBloc),
          ),
        );
      },
    );
  }
}
