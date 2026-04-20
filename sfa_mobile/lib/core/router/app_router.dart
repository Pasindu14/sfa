import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';
import 'package:uswatte/core/router/go_router_refresh_stream.dart';
import 'package:uswatte/features/auth/domain/entities/user_role.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:uswatte/features/auth/presentation/pages/login_page.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_current_route_id_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_outlets_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/sync_outlets_usecase.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_bloc.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_event.dart';
import 'package:uswatte/features/outlets/presentation/pages/outlets_page.dart';
import 'package:uswatte/features/pricing/domain/usecases/get_pricing_usecase.dart';
import 'package:uswatte/features/pricing/domain/usecases/sync_pricing_usecase.dart';
import 'package:uswatte/features/pricing/presentation/bloc/pricing_bloc.dart';
import 'package:uswatte/features/pricing/presentation/bloc/pricing_event.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';
import 'package:uswatte/features/pricing/presentation/pages/pricing_page.dart';
import 'package:uswatte/features/pricing/presentation/pages/pricing_structure_detail_page.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/delete_assignment_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_assignments_usecase.dart';
import 'package:uswatte/features/route_assignment/presentation/bloc/assignments_bloc.dart';
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
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/features/bills/domain/usecases/create_bill_usecase.dart';
import 'package:uswatte/features/pricing/data/datasources/pricing_local_datasource.dart';
import 'package:uswatte/features/bills/domain/usecases/delete_bill_usecase.dart';
import 'package:uswatte/features/bills/domain/usecases/get_bills_usecase.dart';
import 'package:uswatte/features/bills/domain/usecases/retry_sync_usecase.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_bloc.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_event.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_bloc.dart';
import 'package:uswatte/features/bills/presentation/pages/bill_detail_page.dart';
import 'package:uswatte/features/bills/presentation/pages/bills_list_page.dart';
import 'package:uswatte/features/bills/presentation/pages/create_bill_page.dart';

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
              builder: (_, __) => MultiBlocProvider(
                providers: [
                  BlocProvider(
                    create: (_) => AssignmentsBloc(
                      getAssignments: getIt<GetAssignmentsUseCase>(),
                      deleteAssignment: getIt<DeleteAssignmentUseCase>(),
                    )..add(LoadAssignmentsRequested(date: DateTime.now())),
                  ),
                  BlocProvider(
                    create: (_) => OutletsBloc(
                      getOutletsUseCase: getIt<GetOutletsUseCase>(),
                      syncOutletsUseCase: getIt<SyncOutletsUseCase>(),
                      getCurrentRouteIdUseCase:
                          getIt<GetCurrentRouteIdUseCase>(),
                    )..add(const LoadOutletsRequested()),
                  ),
                ],
                child: const SalesRepHomePage(),
              ),
            ),
            GoRoute(
              path: 'sync',
              name: 'sync',
              builder: (_, __) => MultiBlocProvider(
                providers: [
                  BlocProvider(
                    create: (_) => ProductsBloc(
                      getProductsUseCase: getIt<GetProductsUseCase>(),
                      syncProductsUseCase: getIt<SyncProductsUseCase>(),
                    )..add(const LoadProductsRequested()),
                  ),
                  BlocProvider(
                    create: (_) => OutletsBloc(
                      getOutletsUseCase: getIt<GetOutletsUseCase>(),
                      syncOutletsUseCase: getIt<SyncOutletsUseCase>(),
                      getCurrentRouteIdUseCase:
                          getIt<GetCurrentRouteIdUseCase>(),
                    )..add(const LoadOutletsRequested()),
                  ),
                  BlocProvider(
                    create: (_) => PricingBloc(
                      getPricingUseCase: getIt<GetPricingUseCase>(),
                      syncPricingUseCase: getIt<SyncPricingUseCase>(),
                    )..add(const LoadPricingRequested()),
                  ),
                ],
                child: const SyncPage(),
              ),
            ),
            GoRoute(
              path: 'outlets',
              name: 'outlets',
              builder: (_, __) => BlocProvider(
                create: (_) => OutletsBloc(
                  getOutletsUseCase: getIt<GetOutletsUseCase>(),
                  syncOutletsUseCase: getIt<SyncOutletsUseCase>(),
                  getCurrentRouteIdUseCase: getIt<GetCurrentRouteIdUseCase>(),
                )..add(const LoadOutletsRequested()),
                child: const OutletsPage(),
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
            GoRoute(
              path: 'pricing',
              name: 'pricing',
              builder: (_, __) => BlocProvider(
                create: (_) => PricingBloc(
                  getPricingUseCase: getIt<GetPricingUseCase>(),
                  syncPricingUseCase: getIt<SyncPricingUseCase>(),
                )..add(const LoadPricingRequested()),
                child: const PricingPage(),
              ),
              routes: [
                GoRoute(
                  path: 'detail',
                  name: 'pricingDetail',
                  builder: (_, state) => PricingStructureDetailPage(
                    structure: state.extra as PricingStructure,
                  ),
                ),
              ],
            ),
            GoRoute(
              path: 'bills',
              name: 'bills',
              builder: (_, __) => BlocProvider(
                create: (_) => BillsListBloc(
                  getBillsUseCase: getIt<GetBillsUseCase>(),
                  retrySyncUseCase: getIt<RetrySyncUseCase>(),
                  deleteBillUseCase: getIt<DeleteBillUseCase>(),
                  syncService: getIt<BillSyncService>(),
                )..add(const LoadBillsRequested()),
                child: const BillsListPage(),
              ),
              routes: [
                GoRoute(
                  path: 'create',
                  name: 'createBill',
                  builder: (_, __) => MultiBlocProvider(
                    providers: [
                      BlocProvider(
                        create: (_) => CreateBillBloc(
                          createBillUseCase: getIt<CreateBillUseCase>(),
                          pricingLocalDatasource: getIt<PricingLocalDatasource>(),
                        ),
                      ),
                      BlocProvider(
                        create: (_) => OutletsBloc(
                          getOutletsUseCase: getIt<GetOutletsUseCase>(),
                          syncOutletsUseCase: getIt<SyncOutletsUseCase>(),
                          getCurrentRouteIdUseCase:
                              getIt<GetCurrentRouteIdUseCase>(),
                        )..add(const LoadOutletsRequested()),
                      ),
                    ],
                    child: const CreateBillPage(),
                  ),
                ),
                GoRoute(
                  path: ':id',
                  name: 'billDetail',
                  builder: (_, state) => BlocProvider(
                    create: (_) => BillsListBloc(
                      getBillsUseCase: getIt<GetBillsUseCase>(),
                      retrySyncUseCase: getIt<RetrySyncUseCase>(),
                      deleteBillUseCase: getIt<DeleteBillUseCase>(),
                      syncService: getIt<BillSyncService>(),
                    ),
                    child: BillDetailPage(
                      clientBillId: state.pathParameters['id']!,
                    ),
                  ),
                ),
              ],
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
