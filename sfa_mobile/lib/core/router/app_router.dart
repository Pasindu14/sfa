import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';
import 'package:uswatte/core/router/go_router_refresh_stream.dart';
import 'package:uswatte/features/auth/domain/entities/user_role.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:uswatte/features/auth/presentation/pages/login_page.dart';
import 'package:uswatte/features/route_assignment/presentation/pages/assignments_list_page.dart';
import 'package:uswatte/features/route_assignment/presentation/pages/route_assignment_page.dart';
import 'package:uswatte/features/sales_rep/presentation/pages/sales_rep_home_page.dart';
import 'package:uswatte/features/products/presentation/bloc/products_bloc.dart';
import 'package:uswatte/features/products/presentation/bloc/products_event.dart';
import 'package:uswatte/features/products/presentation/pages/products_page.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/features/products/domain/usecases/get_products_usecase.dart';
import 'package:uswatte/features/products/domain/usecases/sync_products_usecase.dart';
import 'package:uswatte/features/sync/presentation/pages/sync_page.dart';
import 'package:uswatte/features/sales_rep/presentation/pages/unsupported_role_page.dart';
import 'package:uswatte/features/splash/presentation/pages/splash_page.dart';
import 'package:uswatte/features/supervisor/presentation/pages/supervisor_home_page.dart';

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

        if (authState is AuthAuthenticated) {
          if (location == '/' || location == '/login') {
            return _homeRouteForRole(authState.role);
          }
          return null;
        }

        // Not authenticated
        if (location != '/login') return '/login';
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
          path: '/sales-rep',
          builder: (_, __) => const SizedBox.shrink(),
          routes: [
            GoRoute(
              path: 'home',
              name: 'salesRepHome',
              builder: (_, __) => const SalesRepHomePage(),
            ),
            GoRoute(
              path: 'sync',
              name: 'sync',
              builder: (_, __) => BlocProvider(
                create: (_) => ProductsBloc(
                  getProductsUseCase: getIt<GetProductsUseCase>(),
                  syncProductsUseCase: getIt<SyncProductsUseCase>(),
                )..add(const LoadProductsRequested()),
                child: const SyncPage(),
              ),
            ),
            GoRoute(
              path: 'products',
              name: 'products',
              builder: (_, __) => BlocProvider(
                create: (_) => ProductsBloc(
                  getProductsUseCase: getIt<GetProductsUseCase>(),
                  syncProductsUseCase: getIt<SyncProductsUseCase>(),
                )..add(const LoadProductsRequested()),
                child: const ProductsPage(),
              ),
            ),
          ],
        ),
        GoRoute(
          path: '/supervisor',
          builder: (_, __) => const SizedBox.shrink(),
          routes: [
            GoRoute(
              path: 'home',
              name: 'supervisorHome',
              builder: (_, __) => const SupervisorHomePage(),
            ),
            GoRoute(
              path: 'assign-route',
              name: 'assignRoute',
              builder: (_, __) => const RouteAssignmentPage(),
            ),
            GoRoute(
              path: 'assignments',
              name: 'assignments',
              builder: (_, __) => const AssignmentsListPage(),
            ),
          ],
        ),
        GoRoute(
          path: '/unsupported-role',
          name: 'unsupportedRole',
          builder: (_, __) => const UnsupportedRolePage(),
        ),
      ],
    );
  }

  static String _homeRouteForRole(UserRole role) {
    switch (role) {
      case UserRole.salesRep:
        return '/sales-rep/home';
      case UserRole.supervisor:
        return '/supervisor/home';
      default:
        return '/unsupported-role';
    }
  }
}
