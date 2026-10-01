// Wire-level check of the lookup the delete flow depends on, against a real
// Dio with a fake adapter. The risky part is what counts as "the server does
// not have this bill": only the API's own 404 error envelope does. Anything
// else (a proxy 404, an API build without the endpoint, a dropped connection)
// says nothing about the bill and must never be read as "not found" — that
// would let the phone delete a bill the server still holds.
import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/bills/data/datasources/bills_remote_datasource.dart';

class _FakeAdapter implements HttpClientAdapter {
  final List<RequestOptions> requests = [];
  Future<ResponseBody> Function(RequestOptions options) respond;

  _FakeAdapter(this.respond);

  @override
  Future<ResponseBody> fetch(RequestOptions options,
      Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    requests.add(options);
    return respond(options);
  }

  @override
  void close({bool force = false}) {}
}

ResponseBody _json(Object body, int status) =>
    ResponseBody.fromString(jsonEncode(body), status, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });

void main() {
  late _FakeAdapter adapter;
  late BillsRemoteDatasource remote;

  setUp(() {
    adapter = _FakeAdapter((_) async => throw StateError('unstubbed'));
    final dio = Dio(BaseOptions(baseUrl: 'http://test'))
      ..httpClientAdapter = adapter;
    remote = BillsRemoteDatasource(dio);
  });

  group('findByClientBillId', () {
    test('200 returns the id and statuses', () async {
      adapter.respond = (_) async => _json({
            'success': true,
            'data': {
              'id': 42,
              'billingNumber': 'B-42',
              'repStatus': 'Submitted',
              'distributorStatus': 'Pending',
            },
          }, 200);

      final found = await remote.findByClientBillId('abc-123');

      expect(found!.id, 42);
      expect(found.billingNumber, 'B-42');
      expect(found.isCancelled, isFalse);
      expect(adapter.requests.single.method, 'GET');
      expect(adapter.requests.single.path,
          '/api/v1/billings/by-client-id/abc-123');
    });

    test('reports a bill the server already cancelled', () async {
      adapter.respond = (_) async => _json({
            'success': true,
            'data': {'id': 7, 'repStatus': 'Cancelled'},
          }, 200);

      expect((await remote.findByClientBillId('x'))!.isCancelled, isTrue);
    });

    test('the API 404 envelope means the server does not have it', () async {
      adapter.respond = (_) async => _json({
            'success': false,
            'error': {
              'code': 'BILLING_NOT_FOUND',
              'message': "Billing with ID 'abc' was not found.",
            },
          }, 404);

      expect(await remote.findByClientBillId('abc'), isNull);
    });

    test('a 404 without the API envelope is NOT "not found"', () async {
      adapter.respond = (_) async => _json({'title': 'Not Found'}, 404);

      await expectLater(remote.findByClientBillId('abc'),
          throwsA(isA<ServerException>()));
    });

    test('a 5xx is surfaced, not read as "not found"', () async {
      adapter.respond = (_) async => _json({
            'success': false,
            'error': {'code': 'INTERNAL_ERROR', 'message': 'boom'},
          }, 500);

      await expectLater(
          remote.findByClientBillId('abc'), throwsA(isA<ServerException>()));
    });

    test('a dropped connection is a NetworkException', () async {
      adapter.respond = (options) async => throw DioException(
            requestOptions: options,
            type: DioExceptionType.connectionError,
          );

      await expectLater(
          remote.findByClientBillId('abc'), throwsA(isA<NetworkException>()));
    });
  });

  group('cancelBilling', () {
    test('keeps the server refusal instead of calling it a network error',
        () async {
      adapter.respond = (_) async => _json({
            'success': false,
            'error': {
              'code': 'INVALID_BILLING_STATE',
              'message': 'Only submitted billings can be cancelled.',
            },
          }, 422);

      await expectLater(
          remote.cancelBilling(5), throwsA(isA<BusinessRuleException>()));
    });

    test('a dropped connection is still a NetworkException', () async {
      adapter.respond = (options) async => throw DioException(
            requestOptions: options,
            type: DioExceptionType.connectionError,
          );

      await expectLater(
          remote.cancelBilling(5), throwsA(isA<NetworkException>()));
    });
  });
}
