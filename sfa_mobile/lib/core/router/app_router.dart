import 'package:go_router/go_router.dart';
import 'package:uswatte/features/auth/presentation/pages/login_page.dart';
import 'package:uswatte/features/dashboard/presentation/pages/dashboard_page.dart';

class AppRouter {
  AppRouter._();

  static final GoRouter router = GoRouter(
    initialLocation: '/login',
    routes: [
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
