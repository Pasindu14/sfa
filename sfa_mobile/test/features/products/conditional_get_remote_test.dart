// Wire-level check of the conditional GET against a real Dio with a fake
// adapter: the header, the per-request 304 acceptance, and ETag parsing.
import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/conditional_get.dart';
import 'package:uswatte/features/products/data/datasources/product_categories_remote_datasource.dart';
import 'package:uswatte/features/products/data/datasources/products_remote_datasource.dart';

class _FakeAdapter implements HttpClientAdapter {
  final List<RequestOptions> requests = [];
  ResponseBody Function(RequestOptions options) respond;

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

ResponseBody _json(Object body, {String? etag}) =>
    ResponseBody.fromString(jsonEncode(body), 200, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
      if (etag != null) 'etag': [etag],
    });

const _categoriesBody = {
  'success': true,
  'traceId': 't',
  'data': {
    'categories': [
      {'id': 1, 'name': 'Soap', 'sortOrder': 0},
    ],
    'totalCount': 1,
    'cachedAt': '2026-09-17T09:30:00Z',
  },
};

const _productsBody = {
  'success': true,
  'traceId': 't',
  'data': {
    'products': <Object>[],
    'totalCount': 0,
    'cachedAt': '2026-09-17T09:30:00Z',
  },
};

void main() {
  late _FakeAdapter adapter;
  late Dio dio;

  setUp(() {
    adapter = _FakeAdapter((_) => _json(_categoriesBody));
    dio = Dio(BaseOptions(baseUrl: 'http://test'))..httpClientAdapter = adapter;
  });

  test('without an ETag the request carries no If-None-Match', () async {
    final result =
        await ProductCategoriesRemoteDatasource(dio).getProductCategories();

    expect(
        adapter.requests.single.headers.containsKey('If-None-Match'), isFalse);
    expect((result as Fetched).etag, isNull);
  });

  test('200 with an ETag returns the data and the ETag', () async {
    adapter.respond = (_) => _json(_categoriesBody, etag: '"c2"');

    final result = await ProductCategoriesRemoteDatasource(dio)
        .getProductCategories(ifNoneMatch: '"c1"');

    expect(adapter.requests.single.headers['If-None-Match'], '"c1"');
    final fetched = result as Fetched<dynamic>;
    expect(fetched.etag, '"c2"');
  });

  test('304 is NotModified, not an error', () async {
    adapter.respond = (_) => ResponseBody.fromString('', 304);

    final result =
        await ProductsRemoteDatasource(dio).getProducts(ifNoneMatch: 'W/"p1"');

    expect(result, isA<NotModified<dynamic>>());
  });

  test('304 on a request that sent no ETag stays an error (old behaviour)',
      () async {
    adapter.respond = (_) => ResponseBody.fromString('', 304);

    await expectLater(ProductsRemoteDatasource(dio).getProducts(),
        throwsA(isA<AppException>()));
  });

  test('200 on the products endpoint parses as before', () async {
    adapter.respond = (_) => _json(_productsBody, etag: '"p2"');

    final result = await ProductsRemoteDatasource(dio).getProducts();

    expect((result as Fetched<dynamic>).etag, '"p2"');
  });

  test(
      'header and 304 acceptance live on RequestOptions, so a token-refresh '
      'retry via fetch(requestOptions) keeps them', () async {
    adapter.respond = (_) => ResponseBody.fromString('', 304);
    await ProductsRemoteDatasource(dio).getProducts(ifNoneMatch: '"p1"');

    // TokenInterceptor retries a 401 with `rawDio.fetch(err.requestOptions)`.
    final original = adapter.requests.single;
    final retryDio = Dio()..httpClientAdapter = adapter;
    final retried = await retryDio.fetch<dynamic>(original);

    expect(retried.statusCode, 304);
    expect(adapter.requests.last.headers['If-None-Match'], '"p1"');
  });
}
