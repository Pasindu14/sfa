// A refresh token the server has definitively rejected must not stay in secure storage, or
// the background location service (no UI to log the rep out) re-sends it every upload window
// forever. A transient failure, or a pair another isolate already rotated, must be left alone.
import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/core/device/device_id_service.dart';
import 'package:uswatte/core/network/session_expired_notifier.dart';
import 'package:uswatte/core/network/token_cache.dart';
import 'package:uswatte/core/network/token_interceptor.dart';

class _MockStorage extends Mock implements FlutterSecureStorage {}

class _MockDeviceId extends Mock implements DeviceIdService {}

class _Adapter implements HttpClientAdapter {
  final ResponseBody Function(RequestOptions) respond;
  _Adapter(this.respond);

  @override
  Future<ResponseBody> fetch(RequestOptions options,
          Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async =>
      respond(options);

  @override
  void close({bool force = false}) {}
}

ResponseBody _json(Object body, int status) =>
    ResponseBody.fromString(jsonEncode(body), status, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });

void main() {
  late _MockStorage storage;
  late SessionExpiredNotifier notifier;
  late int expiredSignals;
  late List<String?> refreshReads;

  /// Builds a client whose every call 401s, so each one goes through a refresh that the
  /// raw client answers with [refreshStatus]. The Nth read of the refresh token returns
  /// `refreshReads[N]` (the last value repeats).
  Dio buildClient(int refreshStatus) {
    var reads = 0;
    when(() => storage.read(key: AppConstants.refreshTokenKey)).thenAnswer((_) async {
      final i = reads < refreshReads.length ? reads : refreshReads.length - 1;
      reads++;
      return refreshReads[i];
    });
    when(() => storage.delete(key: any(named: 'key'))).thenAnswer((_) async {});

    final deviceId = _MockDeviceId();
    when(() => deviceId.getDeviceId()).thenAnswer((_) async => 'dev-1');

    final interceptor = TokenInterceptor(
      storage,
      TokenCache()..update('old'),
      deviceId,
      notifier,
      rawDioFactory: () => Dio(BaseOptions(baseUrl: 'http://test'))
        ..httpClientAdapter =
            _Adapter((_) => _json({'code': 'AUTH_INVALID_TOKEN'}, refreshStatus)),
    );

    return Dio(BaseOptions(baseUrl: 'http://test'))
      ..httpClientAdapter = _Adapter((_) => _json({'code': 'UNAUTHORIZED'}, 401))
      ..interceptors.add(interceptor);
  }

  setUp(() {
    storage = _MockStorage();
    notifier = SessionExpiredNotifier();
    expiredSignals = 0;
    notifier.stream.listen((_) => expiredSignals++);
  });

  test('a 401 from the refresh endpoint clears the dead token pair', () async {
    refreshReads = ['dead', 'dead']; // read to send it, read again to compare
    final client = buildClient(401);

    await expectLater(client.get<dynamic>('/x'), throwsA(isA<DioException>()));
    await Future<void>.delayed(Duration.zero);

    verify(() => storage.delete(key: AppConstants.accessTokenKey)).called(1);
    verify(() => storage.delete(key: AppConstants.refreshTokenKey)).called(1);
    expect(expiredSignals, 1, reason: 'the UI must still be told to show login');
  });

  test('a 403 from the refresh endpoint is also a definitive rejection', () async {
    refreshReads = ['dead', 'dead'];
    final client = buildClient(403);

    await expectLater(client.get<dynamic>('/x'), throwsA(isA<DioException>()));

    verify(() => storage.delete(key: AppConstants.refreshTokenKey)).called(1);
  });

  test('a pair another isolate already rotated is NOT deleted', () async {
    // First read: the stale token we sent. Second read: the fresh one the other isolate stored.
    refreshReads = ['stale', 'fresh'];
    final client = buildClient(401);

    await expectLater(client.get<dynamic>('/x'), throwsA(isA<DioException>()));

    verifyNever(() => storage.delete(key: any(named: 'key')));
  });

  test('a server error on refresh says nothing about the token and keeps it', () async {
    refreshReads = ['good', 'good'];
    final client = buildClient(503);

    await expectLater(client.get<dynamic>('/x'), throwsA(isA<DioException>()));

    verifyNever(() => storage.delete(key: any(named: 'key')));
  });
}
