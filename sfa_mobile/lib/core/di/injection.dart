import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:get_it/get_it.dart';
import 'package:uswatte/core/network/dio_client.dart';
import 'package:uswatte/core/network/token_cache.dart';
import 'package:uswatte/features/auth/data/datasources/auth_local_datasource.dart';
import 'package:uswatte/features/auth/data/datasources/auth_remote_datasource.dart';
import 'package:uswatte/features/auth/data/repositories/auth_repository_impl.dart';
import 'package:uswatte/features/auth/domain/repositories/auth_repository.dart';
import 'package:uswatte/features/auth/domain/usecases/get_current_auth_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/login_usecase.dart';
import 'package:uswatte/features/auth/domain/usecases/logout_usecase.dart';

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
}
