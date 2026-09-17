// Conditional GET for the products / categories master data: a stored ETag is
// only sent when there is a local table for a 304 to keep, a 304 never touches
// the table, and an ETag is only recorded once its rows have been written.
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/conditional_get.dart';
import 'package:uswatte/core/sync/etag_store.dart';
import 'package:uswatte/features/products/data/datasources/product_categories_local_datasource.dart';
import 'package:uswatte/features/products/data/datasources/product_categories_remote_datasource.dart';
import 'package:uswatte/features/products/data/datasources/products_local_datasource.dart';
import 'package:uswatte/features/products/data/datasources/products_remote_datasource.dart';
import 'package:uswatte/features/products/data/models/product_category_model.dart';
import 'package:uswatte/features/products/data/models/product_model.dart';
import 'package:uswatte/features/products/data/repositories/product_categories_repository_impl.dart';
import 'package:uswatte/features/products/data/repositories/products_repository_impl.dart';

class _MockProductsRemote extends Mock implements ProductsRemoteDatasource {}

class _MockProductsLocal extends Mock implements ProductsLocalDatasource {}

class _MockCategoriesRemote extends Mock
    implements ProductCategoriesRemoteDatasource {}

class _MockCategoriesLocal extends Mock
    implements ProductCategoriesLocalDatasource {}

class _MockEtags extends Mock implements EtagStore {}

final _now = DateTime.utc(2026, 9, 17, 10);
final _serverCachedAt = DateTime.utc(2026, 9, 17, 9, 30);

const _category = ProductCategoryModel(id: 1, name: 'Soap', sortOrder: 0);

ProductCategoryListResponseModel _categories() =>
    ProductCategoryListResponseModel(
      categories: const [_category],
      totalCount: 1,
      cachedAt: _serverCachedAt,
    );

void main() {
  setUpAll(() {
    registerFallbackValue(DateTime(2000));
    registerFallbackValue(<ProductModel>[]);
    registerFallbackValue(<ProductCategoryModel>[]);
  });

  group('ProductCategoriesRepositoryImpl.syncCategories', () {
    late _MockCategoriesRemote remote;
    late _MockCategoriesLocal local;
    late _MockEtags etags;
    late ProductCategoriesRepositoryImpl repo;
    final log = <String>[];

    void remoteReturns(ConditionalResponse<ProductCategoryListResponseModel> r) {
      when(() => remote.getProductCategories(
          ifNoneMatch: any(named: 'ifNoneMatch'))).thenAnswer((_) async => r);
    }

    setUp(() {
      log.clear();
      remote = _MockCategoriesRemote();
      local = _MockCategoriesLocal();
      etags = _MockEtags();
      repo = ProductCategoriesRepositoryImpl(remote, local, etags,
          clock: () => _now);

      when(() => etags.read(EtagStore.productCategories))
          .thenAnswer((_) async => '"v1"');
      when(() => etags.write(any(), any())).thenAnswer((i) async {
        log.add('write ${i.positionalArguments[1]}');
      });
      when(() => etags.clear(any())).thenAnswer((_) async => log.add('clear'));
      when(() => local.hasAny()).thenAnswer((_) async => true);
      when(() => local.getAll()).thenAnswer((_) async => const [_category]);
      when(() => local.replaceAll(any()))
          .thenAnswer((_) async => log.add('replace'));
      when(() => local.saveLastSyncedAt(any())).thenAnswer((_) async {});
    });

    test('304 keeps the local table and does not touch the stored ETag',
        () async {
      remoteReturns(const NotModified());

      final (categories, syncedAt) = await repo.syncCategories();

      verify(() => remote.getProductCategories(ifNoneMatch: '"v1"')).called(1);
      verifyNever(() => local.replaceAll(any()));
      verifyNever(() => etags.write(any(), any()));
      verifyNever(() => etags.clear(any()));
      expect(categories.single.name, 'Soap');
      expect(syncedAt, _now);
      verify(() => local.saveLastSyncedAt(_now)).called(1);
    });

    test('200 replaces the table, then stores the new ETag', () async {
      remoteReturns(Fetched(_categories(), '"v2"'));

      final (_, syncedAt) = await repo.syncCategories();

      expect(log, ['clear', 'replace', 'write "v2"']);
      expect(syncedAt, _serverCachedAt);
      verify(() => local.saveLastSyncedAt(_serverCachedAt)).called(1);
    });

    test('200 without an ETag (old server) forgets the stored one', () async {
      remoteReturns(Fetched(_categories(), null));

      await repo.syncCategories();

      expect(log, ['clear', 'replace']);
    });

    test('a failed replace never records the new ETag', () async {
      remoteReturns(Fetched(_categories(), '"v2"'));
      when(() => local.replaceAll(any())).thenThrow(Exception('disk full'));

      await expectLater(repo.syncCategories(), throwsException);
      verifyNever(() => etags.write(any(), any()));
    });

    test('no stored ETag sends no If-None-Match', () async {
      when(() => etags.read(EtagStore.productCategories))
          .thenAnswer((_) async => null);
      remoteReturns(Fetched(_categories(), '"v2"'));

      await repo.syncCategories();

      verify(() => remote.getProductCategories(ifNoneMatch: null)).called(1);
    });

    test('an empty local table sends no If-None-Match', () async {
      when(() => local.hasAny()).thenAnswer((_) async => false);
      remoteReturns(Fetched(_categories(), '"v2"'));

      await repo.syncCategories();

      verify(() => remote.getProductCategories(ifNoneMatch: null)).called(1);
    });

    test('force sends no If-None-Match even with a stored ETag', () async {
      remoteReturns(Fetched(_categories(), '"v2"'));

      await repo.syncCategories(force: true);

      verify(() => remote.getProductCategories(ifNoneMatch: null)).called(1);
      verifyNever(() => etags.read(any()));
    });

    test('network errors propagate unchanged', () async {
      when(() => remote.getProductCategories(
              ifNoneMatch: any(named: 'ifNoneMatch')))
          .thenThrow(const NetworkException(message: 'offline'));

      await expectLater(
          repo.syncCategories(), throwsA(isA<NetworkException>()));
      verifyNever(() => local.replaceAll(any()));
    });
  });

  group('ProductsRepositoryImpl.syncProducts', () {
    late _MockProductsRemote remote;
    late _MockProductsLocal local;
    late _MockEtags etags;
    late ProductsRepositoryImpl repo;

    setUp(() {
      remote = _MockProductsRemote();
      local = _MockProductsLocal();
      etags = _MockEtags();
      repo = ProductsRepositoryImpl(remote, local, etags, clock: () => _now);

      when(() => etags.read(EtagStore.products))
          .thenAnswer((_) async => 'W/"p1"');
      when(() => etags.write(any(), any())).thenAnswer((_) async {});
      when(() => etags.clear(any())).thenAnswer((_) async {});
      when(() => local.hasAny()).thenAnswer((_) async => true);
      when(() => local.getAllProducts()).thenAnswer((_) async => const []);
      when(() => local.replaceAll(any())).thenAnswer((_) async {});
      when(() => local.saveLastSyncedAt(any())).thenAnswer((_) async {});
    });

    test('304 does not replace the products table', () async {
      when(() => remote.getProducts(ifNoneMatch: any(named: 'ifNoneMatch')))
          .thenAnswer((_) async => const NotModified());

      await repo.syncProducts();

      verify(() => remote.getProducts(ifNoneMatch: 'W/"p1"')).called(1);
      verifyNever(() => local.replaceAll(any()));
      verify(() => local.getAllProducts()).called(1);
    });

    test('200 replaces the table and then stores the ETag', () async {
      when(() => remote.getProducts(ifNoneMatch: any(named: 'ifNoneMatch')))
          .thenAnswer((_) async => Fetched(
                ProductListResponseModel(
                    products: const [],
                    totalCount: 0,
                    cachedAt: _serverCachedAt),
                '"p2"',
              ));

      await repo.syncProducts();

      verifyInOrder([
        () => etags.clear(EtagStore.products),
        () => local.replaceAll(any()),
        () => etags.write(EtagStore.products, '"p2"'),
      ]);
    });

    test('empty table or missing ETag sends no If-None-Match', () async {
      when(() => remote.getProducts(ifNoneMatch: any(named: 'ifNoneMatch')))
          .thenAnswer((_) async => const NotModified());

      when(() => local.hasAny()).thenAnswer((_) async => false);
      await repo.syncProducts();
      when(() => local.hasAny()).thenAnswer((_) async => true);
      when(() => etags.read(EtagStore.products)).thenAnswer((_) async => null);
      await repo.syncProducts();

      verify(() => remote.getProducts(ifNoneMatch: null)).called(2);
    });

    test('overlapping syncs run one after another', () async {
      var active = 0;
      var maxActive = 0;
      when(() => remote.getProducts(ifNoneMatch: any(named: 'ifNoneMatch')))
          .thenAnswer((_) async {
        active++;
        if (active > maxActive) maxActive = active;
        await Future<void>.delayed(const Duration(milliseconds: 5));
        active--;
        return const NotModified();
      });

      await Future.wait([repo.syncProducts(), repo.syncProducts()]);

      expect(maxActive, 1);
      verify(() => remote.getProducts(ifNoneMatch: any(named: 'ifNoneMatch')))
          .called(2);
    });
  });
}
