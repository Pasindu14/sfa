import 'dart:io';
import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:uswatte/core/device/device_id_service.dart';
import 'package:uswatte/core/env/app_env.dart';
import 'package:uswatte/core/network/session_expired_notifier.dart';
import 'package:uswatte/core/network/token_cache.dart';
import 'package:uswatte/core/network/token_interceptor.dart';

Dio createDioClient(
  FlutterSecureStorage storage,
  TokenCache cache,
  DeviceIdService deviceIdService,
  SessionExpiredNotifier sessionExpiredNotifier,
) {
  final dio = Dio(
    BaseOptions(
      baseUrl: AppEnv.apiBaseUrl,
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 15),
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    ),
  );

  // In debug builds the API runs with a self-signed dev cert.
  // This bypass is compiled out entirely in release/profile builds.
  if (kDebugMode) {
    (dio.httpClientAdapter as IOHttpClientAdapter).createHttpClient = () {
      final client = HttpClient();
      client.badCertificateCallback = (_, __, ___) => true;
      return client;
    };
  }

  dio.interceptors.add(
    TokenInterceptor(storage, cache, deviceIdService, sessionExpiredNotifier),
  );

  return dio;
}
