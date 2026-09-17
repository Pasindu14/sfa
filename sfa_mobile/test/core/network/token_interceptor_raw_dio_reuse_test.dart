// T3.26: the interceptor-free Dio used for token refresh and for retrying the
// original request is created once and reused, and a retry still carries the
// request's own headers, validateStatus and extras.
import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/core/device/device_id_service.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/session_expired_notifier.dart';
import 'package:uswatte/core/network/token_cache.dart';
import 'package:uswatte/core/network/token_interceptor.dart';

class _MockStorage extends Mock implements FlutterSecureStorage {}

class _MockDeviceId extends Mock implements DeviceIdService {}

class _Adapter implements HttpClientAdapter {
  final List<RequestOptions> requests = [];
  final ResponseBody Function(RequestOptions) respond;
  _Adapter(this.respond);

  @override
  Future<ResponseBody> fetch(RequestOptions options,
      Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    requests.add(options);
    return respond(options);
  }

  @override
  void close({bool force = false}) {}
}

ResponseBody _json(Object body, [int status = 200]) =>
    ResponseBody.fromString(jsonEncode(body), status, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });

void main() {
  late _MockStorage storage;
  late _MockDeviceId deviceId;
  late TokenCache cache;
  late SessionExpiredNotifier notifier;
  late int refreshCount;
  late int factoryCalls;
  late List<Dio> built;
  late _Adapter rawAdapter;
  late Dio main;

  setUp(() {
    storage = _MockStorage();
    deviceId = _MockDeviceId();
    cache = TokenCache()..update('old');
    notifier = SessionExpiredNotifier();
    refreshCount = 0;
    factoryCalls = 0;
    built = [];

    when(() => storage.read(key: AppConstants.refreshTokenKey))
        .thenAnswer((_) async => 'r0');
    when(() => storage.write(key: any(named: 'key'), value: any(named: 'value')))
        .thenAnswer((_) async {});
    when(() => deviceId.getDeviceId()).thenAnswer((_) async => 'dev-1');

    rawAdapter = _Adapter((o) {
      if (o.path == '/api/v1/auth/refresh') {
        refreshCount++;
        return _json({
          'data': {'accessToken': 'new$refreshCount', 'refreshToken': 'r$refreshCount'}
        });
      }
      if (o.path == '/etag') return ResponseBody.fromString('', 304);
      return _json({'ok': true});
    });

    final interceptor = TokenInterceptor(storage, cache, deviceId, notifier,
        rawDioFactory: () {
      factoryCalls++;
      final d = Dio(BaseOptions(baseUrl: 'http://test'))
        ..httpClientAdapter = rawAdapter;
      built.add(d);
      return d;
    });

    // The main client always answers 401, so every call goes through a
    // refresh and a retry on the raw Dio.
    main = Dio(BaseOptions(baseUrl: 'http://test'))
      ..httpClientAdapter = _Adapter((_) => _json({'code': 'UNAUTHORIZED'}, 401))
      ..interceptors.add(interceptor);
  });

  test('two refreshes and retries share one raw Dio instance', () async {
    final a = await main.get<dynamic>('/a');
    final b = await main.get<dynamic>('/b');

    expect(a.statusCode, 200);
    expect(b.statusCode, 200);
    expect(refreshCount, 2);
    expect(factoryCalls, 1);
    expect(built, hasLength(1));
    expect(rawAdapter.requests.map((r) => r.path),
        ['/api/v1/auth/refresh', '/a', '/api/v1/auth/refresh', '/b']);
    // Refresh body and the retried token are unchanged.
    expect(rawAdapter.requests[0].data,
        {'refreshToken': 'r0', 'deviceId': 'dev-1'});
    expect(rawAdapter.requests[1].headers['Authorization'], 'Bearer new1');
    expect(rawAdapter.requests[3].headers['Authorization'], 'Bearer new2');
    expect(cache.accessToken, 'new2');
  });

  test('retry keeps If-None-Match, validateStatus, Idempotency-Key and extra',
      () async {
    final res = await main.get<dynamic>(
      '/etag',
      options: Options(
        headers: {'If-None-Match': '"p1"', 'X-Idempotency-Key': 'bill-1'},
        validateStatus: (s) => s != null && (s == 304 || (s >= 200 && s < 300)),
        extra: {'tag': 'x'},
      ),
    );

    expect(res.statusCode, 304);
    final retried = rawAdapter.requests.last;
    expect(retried.path, '/etag');
    expect(retried.headers['If-None-Match'], '"p1"');
    expect(retried.headers['X-Idempotency-Key'], 'bill-1');
    expect(retried.extra['tag'], 'x');
  });

  test('refresh failure still notifies session expiry and rejects', () async {
    when(() => storage.read(key: AppConstants.refreshTokenKey))
        .thenAnswer((_) async => null);
    var expired = 0;
    final sub = notifier.stream.listen((_) => expired++);

    await expectLater(
      main.get<dynamic>('/a'),
      throwsA(isA<DioException>()
          .having((e) => e.error, 'error', isA<UnauthorizedException>())),
    );
    await Future<void>.delayed(Duration.zero);

    expect(expired, 1);
    expect(cache.accessToken, isNull);
    await sub.cancel();
  });
}
