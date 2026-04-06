import 'package:go_router/go_router.dart';
import 'package:uswatte/core/router/go_router_refresh_stream.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:uswatte/features/auth/presentation/pages/login_page.dart';
import 'package:uswatte/features/dashboard/presentation/pages/dashboard_page.dart';
import 'package:uswatte/features/splash/presentation/pages/splash_page.dart';

class AppRouter {
  AppRouter._();

  static GoRouter createRouter(AuthBloc authBloc) {
    return GoRouter(
      // Start at splash; redirect fires immediately via refreshListenable
      initialLocation: '/',
      refreshListenable: GoRouterRefreshStream(authBloc.stream),
      redirect: (context, state) {
        final authState = authBloc.state;
        final location = state.matchedLocation;

        // AuthInitial = token check in progress — hold on splash
        if (authState is AuthInitial) {
          return location == '/' ? null : '/';
        }

        final isAuthenticated = authState is AuthAuthenticated;

        if (isAuthenticated && (location == '/' || location == '/login')) {
          return '/dashboard';
        }
        if (!isAuthenticated && location != '/login') {
          return '/login';
        }
        return null;
      },
      routes: [
        GoRoute(
          path: '/',
          name: 'splash',
          builder: (context, state) => const SplashPage(),
        ),
        GoRoute(
          path: '/login',
          name: 'login',
          builder: (context, state) => const LoginPage(),
        ),
        GoRoute(
          path: '/dashboard',
          name: 'dashboard',
          builder: (context, state) => const DashboardPage(),
        ),
      ],
    );
  }
}
