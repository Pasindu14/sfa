import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:get_it/get_it.dart';
import 'package:uswatte/core/device/device_id_service.dart';
import 'package:uswatte/core/network/dio_client.dart';
import 'package:uswatte/core/network/session_expired_notifier.dart';
import 'package:uswatte/core/network/token_cache.dart';
import 'package:uswatte/features/auth/data/datasources/auth_local_datasource.dart';
import 'package:uswatte/features/auth/data/datasources/auth_remote_datasource.dart';
import 'package:uswatte/features/auth/data/repositories/auth_repository_impl.dart';
import 'package:uswatte/features/auth/domain/repositories/auth_repository.dart';
import 'package:uswatte/features/auth/domain/usecases/get_current_auth_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/login_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/logout_usecase.dart';
import 'package:uswatte/features/route_assignment/data/datasources/route_assignment_remote_datasource.dart';
import 'package:uswatte/features/route_assignment/data/repositories/route_assignment_repository_impl.dart';
import 'package:uswatte/features/route_assignment/domain/repositories/route_assignment_repository.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/create_assignment_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/delete_assignment_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_assignments_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_rep_routes_usecase.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/products/data/datasources/products_local_datasource.dart';
import 'package:uswatte/features/products/data/datasources/products_remote_datasource.dart';
import 'package:uswatte/features/products/data/repositories/products_repository_impl.dart';
import 'package:uswatte/features/products/domain/repositories/products_repository.dart';
import 'package:uswatte/features/products/domain/usecases/get_products_usecase.dart';
import 'package:uswatte/features/products/domain/usecases/sync_products_usecase.dart';
import 'package:uswatte/features/products/data/datasources/product_categories_local_datasource.dart';
import 'package:uswatte/features/products/data/datasources/product_categories_remote_datasource.dart';
import 'package:uswatte/features/products/data/repositories/product_categories_repository_impl.dart';
import 'package:uswatte/features/products/domain/repositories/product_categories_repository.dart';
import 'package:uswatte/features/products/domain/usecases/get_product_categories_usecase.dart';
import 'package:uswatte/features/products/domain/usecases/sync_product_categories_usecase.dart';
import 'package:uswatte/features/products/presentation/bloc/product_categories_bloc.dart';
import 'package:uswatte/features/outlets/data/datasources/outlets_local_datasource.dart';
import 'package:uswatte/features/outlets/data/datasources/outlets_remote_datasource.dart';
import 'package:uswatte/features/outlets/data/repositories/outlets_repository_impl.dart';
import 'package:uswatte/features/outlets/domain/repositories/outlets_repository.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_outlets_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_current_route_id_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_outlets_last_synced_at_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/sync_outlets_usecase.dart';
import 'package:uswatte/features/pricing/data/datasources/pricing_local_datasource.dart';
import 'package:uswatte/features/pricing/data/datasources/pricing_remote_datasource.dart';
import 'package:uswatte/features/pricing/data/repositories/pricing_repository_impl.dart';
import 'package:uswatte/features/pricing/domain/repositories/pricing_repository.dart';
import 'package:uswatte/features/pricing/domain/usecases/get_pricing_usecase.dart';
import 'package:uswatte/features/pricing/domain/usecases/sync_pricing_usecase.dart';
import 'package:uswatte/core/connectivity/connectivity_service.dart';
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/data/datasources/bills_remote_datasource.dart';
import 'package:uswatte/features/bills/data/repositories/bills_repository_impl.dart';
import 'package:uswatte/features/bills/domain/repositories/bills_repository.dart';
import 'package:uswatte/features/bills/domain/usecases/create_bill_usecase.dart';
import 'package:uswatte/features/bills/domain/usecases/delete_bill_usecase.dart';
import 'package:uswatte/features/bills/domain/usecases/get_bill_by_id_usecase.dart';
import 'package:uswatte/features/bills/domain/usecases/get_bills_usecase.dart';
import 'package:uswatte/features/bills/domain/usecases/retry_sync_usecase.dart';
import 'package:uswatte/features/bills/domain/usecases/search_products_for_bill_usecase.dart';
import 'package:uswatte/features/rep_assignment/data/datasources/rep_assignment_remote_datasource.dart';
import 'package:uswatte/features/rep_assignment/data/repositories/rep_assignment_repository_impl.dart';
import 'package:uswatte/features/rep_assignment/domain/repositories/rep_assignment_repository.dart';
import 'package:uswatte/features/rep_assignment/domain/usecases/get_rep_assignment_usecase.dart';
import 'package:uswatte/features/create_outlet/data/datasources/create_outlet_remote_datasource.dart';
import 'package:uswatte/features/create_outlet/data/repositories/create_outlet_repository_impl.dart';
import 'package:uswatte/features/create_outlet/domain/repositories/create_outlet_repository.dart';
import 'package:uswatte/features/create_outlet/domain/usecases/create_outlet_usecase.dart';
import 'package:uswatte/features/outlet_bill_history/data/datasources/outlet_bill_history_remote_datasource.dart';
import 'package:uswatte/core/sync/not_billing_sync_service.dart';
import 'package:uswatte/features/not_billings/data/datasources/not_billings_local_datasource.dart';
import 'package:uswatte/features/not_billings/data/datasources/not_billings_remote_datasource.dart';
import 'package:uswatte/features/not_billings/data/repositories/not_billings_repository_impl.dart';
import 'package:uswatte/features/not_billings/domain/repositories/not_billings_repository.dart';
import 'package:uswatte/features/not_billings/domain/usecases/create_not_billing_usecase.dart';
import 'package:uswatte/features/not_billings/domain/usecases/delete_not_billing_usecase.dart';
import 'package:uswatte/features/not_billings/domain/usecases/get_not_billing_by_id_usecase.dart';
import 'package:uswatte/features/not_billings/domain/usecases/get_not_billings_usecase.dart';
import 'package:uswatte/features/not_billings/domain/usecases/retry_not_billing_sync_usecase.dart';
import 'package:uswatte/features/outlet_billings/data/datasources/outlet_billings_remote_datasource.dart';
import 'package:uswatte/features/outlet_billings/data/repositories/outlet_billings_repository_impl.dart';
import 'package:uswatte/features/outlet_billings/domain/repositories/outlet_billings_repository.dart';
import 'package:uswatte/features/outlet_billings/domain/usecases/get_assigned_routes_usecase.dart';
import 'package:uswatte/features/outlet_billings/domain/usecases/get_outlet_summary_usecase.dart';
import 'package:uswatte/features/outlet_billings/presentation/cubit/outlet_billings_cubit.dart';
import 'package:uswatte/features/supervisor_billing/data/datasources/supervisor_billing_remote_datasource.dart';
import 'package:uswatte/features/supervisor_not_billing/data/datasources/supervisor_not_billing_remote_datasource.dart';
import 'package:uswatte/features/supervisor_not_billing/data/repositories/supervisor_not_billing_repository_impl.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/repositories/supervisor_not_billing_repository.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/usecases/get_not_billing_detail_usecase.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/usecases/get_supervisor_not_billings_usecase.dart';
import 'package:uswatte/features/supervisor_billing/data/repositories/supervisor_billing_repository_impl.dart';
import 'package:uswatte/features/supervisor_billing/domain/repositories/supervisor_billing_repository.dart';
import 'package:uswatte/features/supervisor_billing/domain/usecases/get_billing_detail_usecase.dart';
import 'package:uswatte/features/supervisor_billing/domain/usecases/get_supervisor_billings_usecase.dart';
import 'package:uswatte/features/supervisor_summary/data/datasources/supervisor_summary_remote_datasource.dart';
import 'package:uswatte/features/supervisor_summary/data/repositories/supervisor_summary_repository_impl.dart';
import 'package:uswatte/features/supervisor_summary/domain/repositories/supervisor_summary_repository.dart';
import 'package:uswatte/features/supervisor_summary/domain/usecases/get_supervisor_summary_usecase.dart';
import 'package:uswatte/features/supervisor_route_map/data/datasources/supervisor_route_map_remote_datasource.dart';
import 'package:uswatte/features/supervisor_route_map/data/repositories/supervisor_route_map_repository_impl.dart';
import 'package:uswatte/features/supervisor_route_map/domain/repositories/supervisor_route_map_repository.dart';
import 'package:uswatte/features/supervisor_route_map/domain/usecases/get_supervisor_route_map_usecase.dart';
import 'package:uswatte/features/todays_route_map/data/repositories/todays_route_map_repository_impl.dart';
import 'package:uswatte/features/todays_route_map/domain/repositories/todays_route_map_repository.dart';
import 'package:uswatte/features/todays_route_map/domain/usecases/get_todays_route_map_usecase.dart';

final getIt = GetIt.instance;

Future<void> configureDependencies() async {
  // ── Infrastructure ──────────────────────────────────────────────────────────
  const storage = FlutterSecureStorage(
    // Encrypts on Android API 23+; falls back to standard on older versions
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
    iOptions: IOSOptions(accessibility: KeychainAccessibility.first_unlock),
  );
  final cache = TokenCache();

  getIt.registerLazySingleton<FlutterSecureStorage>(() => storage);
  getIt.registerLazySingleton<TokenCache>(() => cache);
  getIt.registerLazySingleton<SessionExpiredNotifier>(() => SessionExpiredNotifier());
  getIt.registerLazySingleton(() => DeviceIdService(getIt<FlutterSecureStorage>()));
  getIt.registerLazySingleton<Dio>(() => createDioClient(
        storage,
        cache,
        getIt<DeviceIdService>(),
        getIt<SessionExpiredNotifier>(),
      ));

  // ── Auth datasources ─────────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => AuthRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton(
      () => AuthLocalDatasource(getIt<FlutterSecureStorage>(), getIt<TokenCache>()));

  // ── Auth repository ──────────────────────────────────────────────────────────
  getIt.registerLazySingleton<AuthRepository>(
    () => AuthRepositoryImpl(
      getIt<AuthRemoteDatasource>(),
      getIt<AuthLocalDatasource>(),
    ),
  );

  // ── Auth use cases ───────────────────────────────────────────────────────────
  getIt.registerLazySingleton(() => LoginUseCase(getIt<AuthRepository>()));
  getIt.registerLazySingleton(() => LogoutUseCase(getIt<AuthRepository>()));
  getIt.registerLazySingleton(
      () => GetCurrentAuthUseCase(getIt<AuthRepository>()));

  // ── Route assignment ─────────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => RouteAssignmentRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<RouteAssignmentRepository>(
    () => RouteAssignmentRepositoryImpl(
        getIt<RouteAssignmentRemoteDatasource>()),
  );
  getIt.registerLazySingleton(
      () => GetMyRepsUseCase(getIt<RouteAssignmentRepository>()));
  getIt.registerLazySingleton(
      () => GetRepRoutesUseCase(getIt<RouteAssignmentRepository>()));
  getIt.registerLazySingleton(
      () => CreateAssignmentUseCase(getIt<RouteAssignmentRepository>()));
  getIt.registerLazySingleton(
      () => GetAssignmentsUseCase(getIt<RouteAssignmentRepository>()));
  getIt.registerLazySingleton(
      () => DeleteAssignmentUseCase(getIt<RouteAssignmentRepository>()));

  // ── Products ─────────────────────────────────────────────────────────────────
  getIt.registerLazySingleton(() => DatabaseHelper.instance);
  getIt.registerLazySingleton(
      () => ProductsLocalDatasource(getIt<DatabaseHelper>()));
  getIt.registerLazySingleton(
      () => ProductsRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<ProductsRepository>(
    () => ProductsRepositoryImpl(
      getIt<ProductsRemoteDatasource>(),
      getIt<ProductsLocalDatasource>(),
    ),
  );
  getIt.registerLazySingleton(
      () => GetProductsUseCase(getIt<ProductsRepository>()));
  getIt.registerLazySingleton(
      () => SyncProductsUseCase(getIt<ProductsRepository>()));

  // ── Product Categories ────────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => ProductCategoriesLocalDatasource(getIt<DatabaseHelper>()));
  getIt.registerLazySingleton(
      () => ProductCategoriesRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<ProductCategoriesRepository>(
    () => ProductCategoriesRepositoryImpl(
      getIt<ProductCategoriesRemoteDatasource>(),
      getIt<ProductCategoriesLocalDatasource>(),
    ),
  );
  getIt.registerLazySingleton(
      () => GetProductCategoriesUseCase(getIt<ProductCategoriesRepository>()));
  getIt.registerLazySingleton(
      () => SyncProductCategoriesUseCase(getIt<ProductCategoriesRepository>()));
  getIt.registerFactory(() => ProductCategoriesBloc(
        getProductCategoriesUseCase: getIt<GetProductCategoriesUseCase>(),
        syncProductCategoriesUseCase: getIt<SyncProductCategoriesUseCase>(),
      ));

  // ── Outlets ──────────────────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => OutletsLocalDatasource(getIt<DatabaseHelper>()));
  getIt.registerLazySingleton(
      () => OutletsRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<OutletsRepository>(
    () => OutletsRepositoryImpl(
      getIt<OutletsRemoteDatasource>(),
      getIt<OutletsLocalDatasource>(),
    ),
  );
  getIt.registerLazySingleton(
      () => GetOutletsUseCase(getIt<OutletsRepository>()));
  getIt.registerLazySingleton(
      () => SyncOutletsUseCase(getIt<OutletsRepository>()));
  getIt.registerLazySingleton(
      () => GetCurrentRouteIdUseCase(getIt<OutletsRepository>()));
  getIt.registerLazySingleton(
      () => GetOutletsLastSyncedAtUseCase(getIt<OutletsRepository>()));

  // ── Pricing ──────────────────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => PricingLocalDatasource(getIt<DatabaseHelper>()));
  getIt.registerLazySingleton(
      () => PricingRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<PricingRepository>(
    () => PricingRepositoryImpl(
      getIt<PricingRemoteDatasource>(),
      getIt<PricingLocalDatasource>(),
    ),
  );
  getIt.registerLazySingleton(
      () => GetPricingUseCase(getIt<PricingRepository>()));
  getIt.registerLazySingleton(
      () => SyncPricingUseCase(getIt<PricingRepository>()));

  // ── Connectivity ─────────────────────────────────────────────────────────────
  getIt.registerLazySingleton<ConnectivityService>(() => ConnectivityService());

  // ── Bills ────────────────────────────────────────────────────────────────────
  // BillsLocalDatasource + BillsRemoteDatasource are simple wrappers. The
  // BillSyncService closes the loop: it pushes pending bills whenever
  // ConnectivityService flips from offline -> online, and it's called
  // fire-and-forget from BillsRepositoryImpl.createBill so the page closes
  // instantly while the POST happens in the background.
  getIt.registerLazySingleton(
      () => BillsLocalDatasource(getIt<DatabaseHelper>()));
  getIt.registerLazySingleton(
      () => BillsRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<BillSyncService>(
    () => BillSyncService(
      getIt<BillsLocalDatasource>(),
      getIt<BillsRemoteDatasource>(),
      getIt<ConnectivityService>(),
    ),
  );
  getIt.registerLazySingleton<BillsRepository>(
    () => BillsRepositoryImpl(
      getIt<BillsLocalDatasource>(),
      getIt<BillSyncService>(),
    ),
  );
  getIt.registerLazySingleton(() => CreateBillUseCase(getIt<BillsRepository>()));
  getIt.registerLazySingleton(() => GetBillsUseCase(getIt<BillsRepository>()));
  getIt.registerLazySingleton(
      () => GetBillByIdUseCase(getIt<BillsRepository>()));
  getIt.registerLazySingleton(() => DeleteBillUseCase(getIt<BillsRepository>()));
  getIt.registerLazySingleton(() => RetrySyncUseCase(getIt<BillsRepository>()));
  getIt.registerLazySingleton(
      () => SearchProductsForBillUseCase(getIt<BillsRepository>()));

  // ── Not Billings ─────────────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => NotBillingsLocalDatasource(getIt<DatabaseHelper>()));
  getIt.registerLazySingleton(
      () => NotBillingsRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<NotBillingSyncService>(
    () => NotBillingSyncService(
      getIt<NotBillingsLocalDatasource>(),
      getIt<NotBillingsRemoteDatasource>(),
      getIt<ConnectivityService>(),
    ),
  );
  getIt.registerLazySingleton<NotBillingsRepository>(
    () => NotBillingsRepositoryImpl(
      getIt<NotBillingsLocalDatasource>(),
      getIt<NotBillingSyncService>(),
    ),
  );
  getIt.registerLazySingleton(
      () => CreateNotBillingUseCase(getIt<NotBillingsRepository>()));
  getIt.registerLazySingleton(
      () => GetNotBillingsUseCase(getIt<NotBillingsRepository>()));
  getIt.registerLazySingleton(
      () => GetNotBillingByIdUseCase(getIt<NotBillingsRepository>()));
  getIt.registerLazySingleton(
      () => DeleteNotBillingUseCase(getIt<NotBillingsRepository>()));
  getIt.registerLazySingleton(
      () => RetryNotBillingSyncUseCase(getIt<NotBillingsRepository>()));

  // ── Create outlet ─────────────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => CreateOutletRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<CreateOutletRepository>(
    () => CreateOutletRepositoryImpl(getIt<CreateOutletRemoteDatasource>()),
  );
  getIt.registerLazySingleton(
      () => CreateOutletUseCase(getIt<CreateOutletRepository>()));

  // ── Outlet bill history ───────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => OutletBillHistoryRemoteDatasource(getIt<Dio>()));

  // ── Rep assignment ────────────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => RepAssignmentRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<RepAssignmentRepository>(
    () => RepAssignmentRepositoryImpl(getIt<RepAssignmentRemoteDatasource>()),
  );
  getIt.registerLazySingleton(
      () => GetRepAssignmentUseCase(getIt<RepAssignmentRepository>()));

  // ── Outlet Billings ───────────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => OutletBillingsRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<OutletBillingsRepository>(
    () => OutletBillingsRepositoryImpl(getIt<OutletBillingsRemoteDatasource>()),
  );
  getIt.registerLazySingleton(
      () => GetAssignedRoutesUseCase(getIt<OutletBillingsRepository>()));
  getIt.registerLazySingleton(
      () => GetOutletSummaryUseCase(getIt<OutletBillingsRepository>()));
  getIt.registerFactory(() => OutletBillingsCubit(
        getIt<GetAssignedRoutesUseCase>(),
        getIt<GetOutletSummaryUseCase>(),
      ));

  // ── Supervisor Not Billing ───────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => SupervisorNotBillingRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<SupervisorNotBillingRepository>(
    () => SupervisorNotBillingRepositoryImpl(
        getIt<SupervisorNotBillingRemoteDatasource>()),
  );
  getIt.registerLazySingleton(
      () => GetSupervisorNotBillingsUseCase(getIt<SupervisorNotBillingRepository>()));
  getIt.registerLazySingleton(
      () => GetNotBillingDetailUseCase(getIt<SupervisorNotBillingRepository>()));

  // ── Supervisor Summary ────────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => SupervisorSummaryRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<SupervisorSummaryRepository>(
    () => SupervisorSummaryRepositoryImpl(
        getIt<SupervisorSummaryRemoteDatasource>()),
  );
  getIt.registerLazySingleton(
      () => GetSupervisorSummaryUseCase(getIt<SupervisorSummaryRepository>()));

  // ── Supervisor Billing ────────────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => SupervisorBillingRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<SupervisorBillingRepository>(
    () => SupervisorBillingRepositoryImpl(
        getIt<SupervisorBillingRemoteDatasource>()),
  );
  getIt.registerLazySingleton(
      () => GetSupervisorBillingsUseCase(getIt<SupervisorBillingRepository>()));
  getIt.registerLazySingleton(
      () => GetBillingDetailUseCase(getIt<SupervisorBillingRepository>()));

  // ── Supervisor Route Map ──────────────────────────────────────────────────
  getIt.registerLazySingleton(
      () => SupervisorRouteMapRemoteDatasource(getIt<Dio>()));
  getIt.registerLazySingleton<SupervisorRouteMapRepository>(
    () => SupervisorRouteMapRepositoryImpl(
        getIt<SupervisorRouteMapRemoteDatasource>()),
  );
  getIt.registerLazySingleton(
      () => GetSupervisorRouteMapUseCase(getIt<SupervisorRouteMapRepository>()));

  // ── Today's Route Map ─────────────────────────────────────────────────────
  getIt.registerLazySingleton<TodaysRouteMapRepository>(
    () => TodaysRouteMapRepositoryImpl(
      outletsDatasource: getIt<OutletsLocalDatasource>(),
      billsDatasource: getIt<BillsLocalDatasource>(),
      notBillingsDatasource: getIt<NotBillingsLocalDatasource>(),
    ),
  );
  getIt.registerLazySingleton(
      () => GetTodaysRouteMapUseCase(getIt<TodaysRouteMapRepository>()));
}
