import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:get_it/get_it.dart';
import 'package:uswatte/core/device/device_id_service.dart';
import 'package:uswatte/core/network/dio_client.dart';
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
  getIt.registerLazySingleton<Dio>(() => createDioClient(storage, cache));
  getIt.registerLazySingleton(() => DeviceIdService(getIt<FlutterSecureStorage>()));

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
}
