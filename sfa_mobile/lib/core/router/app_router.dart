import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';
import 'package:uswatte/core/router/go_router_refresh_stream.dart';
import 'package:uswatte/features/auth/domain/entities/user_role.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:uswatte/features/auth/presentation/pages/login_page.dart';
import 'package:uswatte/features/create_outlet/domain/usecases/create_outlet_usecase.dart';
import 'package:uswatte/features/create_outlet/presentation/bloc/create_outlet_bloc.dart';
import 'package:uswatte/features/create_outlet/presentation/pages/create_outlet_page.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_current_route_id_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_outlets_last_synced_at_usecase.dart';
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
import 'package:uswatte/features/rep_assignment/domain/usecases/get_rep_assignment_usecase.dart';
import 'package:uswatte/features/rep_assignment/presentation/bloc/rep_assignment_bloc.dart';
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
import 'package:uswatte/features/debug/presentation/pages/debug_page.dart';
import 'package:uswatte/core/sync/not_billing_sync_service.dart';
import 'package:uswatte/features/not_billings/domain/usecases/create_not_billing_usecase.dart';
import 'package:uswatte/features/not_billings/domain/usecases/delete_not_billing_usecase.dart';
import 'package:uswatte/features/not_billings/domain/usecases/get_not_billings_usecase.dart';
import 'package:uswatte/features/not_billings/domain/usecases/retry_not_billing_sync_usecase.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/create_not_billing_bloc.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/not_billings_list_bloc.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/not_billings_list_event.dart';
import 'package:uswatte/features/not_billings/presentation/pages/create_not_billing_page.dart';
import 'package:uswatte/features/not_billings/presentation/pages/not_billing_detail_page.dart';
import 'package:uswatte/features/not_billings/presentation/pages/not_billings_list_page.dart';
import 'package:uswatte/features/outlet_bill_history/data/datasources/outlet_bill_history_remote_datasource.dart';
import 'package:uswatte/features/outlet_bill_history/data/repositories/outlet_bill_history_repository_impl.dart';
import 'package:uswatte/features/outlet_bill_history/presentation/cubit/outlet_bill_detail_cubit.dart';
import 'package:uswatte/features/outlet_bill_history/presentation/cubit/outlet_bill_history_cubit.dart';
import 'package:uswatte/features/outlet_bill_history/presentation/pages/outlet_bill_detail_page.dart';
import 'package:uswatte/features/outlet_bill_history/presentation/pages/outlet_bill_history_page.dart';
import 'package:uswatte/features/outlet_billings/presentation/pages/outlet_billings_page.dart';
import 'package:uswatte/features/supervisor_billing/domain/usecases/get_billing_detail_usecase.dart';
import 'package:uswatte/features/supervisor_billing/domain/usecases/get_supervisor_billings_usecase.dart';
import 'package:uswatte/features/supervisor_billing/presentation/bloc/supervisor_billing_bloc.dart';
import 'package:uswatte/features/supervisor_billing/presentation/bloc/supervisor_billing_event.dart';
import 'package:uswatte/features/supervisor_billing/presentation/cubit/billing_detail_cubit.dart';
import 'package:uswatte/features/supervisor_billing/presentation/pages/billing_detail_page.dart';
import 'package:uswatte/features/supervisor_billing/presentation/pages/supervisor_billing_page.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';

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
                      getOutletsLastSyncedAtUseCase:
                          getIt<GetOutletsLastSyncedAtUseCase>(),
                    )..add(const LoadOutletsRequested()),
                  ),
                  BlocProvider(
                    create: (_) => RepAssignmentBloc(
                      getRepAssignment: getIt<GetRepAssignmentUseCase>(),
                    )..add(const LoadRepAssignmentRequested()),
                  ),
                  BlocProvider(
                    create: (_) => BillsListBloc(
                      getBillsUseCase: getIt<GetBillsUseCase>(),
                      retrySyncUseCase: getIt<RetrySyncUseCase>(),
                      deleteBillUseCase: getIt<DeleteBillUseCase>(),
                      syncService: getIt<BillSyncService>(),
                    )..add(const LoadBillsRequested()),
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
                    create: (_) => AssignmentsBloc(
                      getAssignments: getIt<GetAssignmentsUseCase>(),
                      deleteAssignment: getIt<DeleteAssignmentUseCase>(),
                    )..add(LoadAssignmentsRequested(date: DateTime.now())),
                  ),
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
                      getOutletsLastSyncedAtUseCase:
                          getIt<GetOutletsLastSyncedAtUseCase>(),
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
                  getOutletsLastSyncedAtUseCase:
                      getIt<GetOutletsLastSyncedAtUseCase>(),
                )..add(const LoadOutletsRequested()),
                child: const OutletsPage(),
              ),
              routes: [
                GoRoute(
                  path: 'create',
                  name: 'createOutlet',
                  builder: (_, __) => BlocProvider(
                    create: (_) => CreateOutletBloc(
                      createOutletUseCase: getIt<CreateOutletUseCase>(),
                      getCurrentRouteIdUseCase:
                          getIt<GetCurrentRouteIdUseCase>(),
                    ),
                    child: const CreateOutletPage(),
                  ),
                ),
              ],
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
                          getOutletsLastSyncedAtUseCase:
                              getIt<GetOutletsLastSyncedAtUseCase>(),
                        )..add(const LoadOutletsRequested()),
                      ),
                    ],
                    child: const CreateBillPage(),
                  ),
                  routes: [
                    GoRoute(
                      path: 'outlet-history',
                      name: 'outletBillHistory',
                      builder: (_, state) {
                        final args =
                            state.extra as Map<String, dynamic>;
                        return BlocProvider(
                          create: (_) => OutletBillHistoryCubit(
                            OutletBillHistoryRepositoryImpl(
                              getIt<OutletBillHistoryRemoteDatasource>(),
                            ),
                          ),
                          child: OutletBillHistoryPage(
                            outletId: args['outletId'] as int,
                            outletName: args['outletName'] as String,
                          ),
                        );
                      },
                      routes: [
                        GoRoute(
                          path: ':billingId',
                          name: 'outletBillDetail',
                          builder: (_, state) => BlocProvider(
                            create: (_) => OutletBillDetailCubit(
                              OutletBillHistoryRepositoryImpl(
                                getIt<OutletBillHistoryRemoteDatasource>(),
                              ),
                            ),
                            child: OutletBillDetailPage(
                              billingId: int.parse(
                                  state.pathParameters['billingId']!),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
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
            GoRoute(
              path: 'not-billings',
              name: 'notBillingsList',
              builder: (_, __) => BlocProvider(
                create: (_) => NotBillingsListBloc(
                  getNotBillingsUseCase: getIt<GetNotBillingsUseCase>(),
                  retrySyncUseCase: getIt<RetryNotBillingSyncUseCase>(),
                  deleteNotBillingUseCase: getIt<DeleteNotBillingUseCase>(),
                  syncService: getIt<NotBillingSyncService>(),
                )..add(const LoadNotBillingsRequested()),
                child: const NotBillingsListPage(),
              ),
              routes: [
                GoRoute(
                  path: 'create',
                  name: 'createNotBilling',
                  builder: (_, __) => MultiBlocProvider(
                    providers: [
                      BlocProvider(
                        create: (_) => CreateNotBillingBloc(
                          createNotBillingUseCase:
                              getIt<CreateNotBillingUseCase>(),
                        ),
                      ),
                      BlocProvider(
                        create: (_) => OutletsBloc(
                          getOutletsUseCase: getIt<GetOutletsUseCase>(),
                          syncOutletsUseCase: getIt<SyncOutletsUseCase>(),
                          getCurrentRouteIdUseCase:
                              getIt<GetCurrentRouteIdUseCase>(),
                          getOutletsLastSyncedAtUseCase:
                              getIt<GetOutletsLastSyncedAtUseCase>(),
                        )..add(const LoadOutletsRequested()),
                      ),
                    ],
                    child: const CreateNotBillingPage(),
                  ),
                ),
                GoRoute(
                  path: ':id',
                  name: 'notBillingDetail',
                  builder: (_, state) => BlocProvider(
                    create: (_) => NotBillingsListBloc(
                      getNotBillingsUseCase: getIt<GetNotBillingsUseCase>(),
                      retrySyncUseCase: getIt<RetryNotBillingSyncUseCase>(),
                      deleteNotBillingUseCase:
                          getIt<DeleteNotBillingUseCase>(),
                      syncService: getIt<NotBillingSyncService>(),
                    )..add(const LoadNotBillingsRequested()),
                    child: NotBillingDetailPage(
                      clientNotBillingId: state.pathParameters['id']!,
                    ),
                  ),
                ),
              ],
            ),
            GoRoute(
              path: 'outlet-billings',
              name: 'outletBillings',
              builder: (_, __) => const OutletBillingsPage(),
            ),
            GoRoute(
              path: 'debug',
              name: 'salesRepDebug',
              builder: (_, __) => const DebugPage(),
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
            GoRoute(
              path: 'billing',
              name: 'supervisorBilling',
              builder: (_, __) => BlocProvider(
                create: (_) => SupervisorBillingBloc(
                  getMyReps: getIt<GetMyRepsUseCase>(),
                  getSupervisorBillings:
                      getIt<GetSupervisorBillingsUseCase>(),
                )..add(const LoadRepsRequested()),
                child: const SupervisorBillingPage(),
              ),
              routes: [
                GoRoute(
                  path: ':id',
                  name: 'billingDetail',
                  builder: (_, state) => BlocProvider(
                    create: (_) => BillingDetailCubit(
                        getIt<GetBillingDetailUseCase>()),
                    child: BillingDetailPage(
                      billingId: int.parse(state.pathParameters['id']!),
                      billingNumber: state.extra as String?,
                    ),
                  ),
                ),
              ],
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
