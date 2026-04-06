import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:uswatte/core/router/app_router.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';

void main() {
  runApp(const SfaApp());
}

class SfaApp extends StatelessWidget {
  const SfaApp({super.key});

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
            BlocProvider<AuthBloc>(create: (_) => AuthBloc()),
          ],
          child: MaterialApp.router(
            title: 'SFA Uswatte',
            debugShowCheckedModeBanner: false,
            theme: AppTheme.light,
            routerConfig: AppRouter.router,
          ),
        );
      },
    );
  }
}
