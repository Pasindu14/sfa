// runSync: master-data downloads run in parallel with every step isolated from
// the others' failures, outlets still wait for the assignment, and stock only
// syncs after the bill flush (which may already have refreshed it).
import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/background/background_sync_service.dart';
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/core/sync/not_billing_sync_service.dart';
import 'package:uswatte/features/outlets/domain/entities/proximity_policy.dart';
import 'package:uswatte/features/outlets/domain/usecases/clear_daily_outlets_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/sync_outlets_usecase.dart';
import 'package:uswatte/features/pricing/domain/usecases/sync_pricing_structures_usecase.dart';
import 'package:uswatte/features/products/domain/usecases/sync_product_categories_usecase.dart';
import 'package:uswatte/features/products/domain/usecases/sync_products_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/entities/daily_route_assignment.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_assignments_usecase.dart';
import 'package:uswatte/features/stock/domain/usecases/sync_distributor_stock_usecase.dart';

class _MockProducts extends Mock implements SyncProductsUseCase {}

class _MockCategories extends Mock implements SyncProductCategoriesUseCase {}

class _MockPricing extends Mock implements SyncPricingStructuresUseCase {}

class _MockOutlets extends Mock implements SyncOutletsUseCase {}

class _MockClearOutlets extends Mock implements ClearDailyOutletsUseCase {}

class _MockStock extends Mock implements SyncDistributorStockUseCase {}

class _MockAssignments extends Mock implements GetAssignmentsUseCase {}

class _MockBillSync extends Mock implements BillSyncService {}

class _MockNotBillingSync extends Mock implements NotBillingSyncService {}

class _MockPolicy extends Mock implements ProximityPolicy {}

final _assignment = DailyRouteAssignment(
  id: 1,
  userId: 7,
  userName: 'Rep',
  routeId: 42,
  routeName: 'Route 42',
  assignedDate: DateTime(2026, 9, 17),
  isActive: true,
  createdAt: DateTime(2026, 9, 17),
);

AssignmentsResult _result(List<DailyRouteAssignment> a) => AssignmentsResult(
    assignments: a, totalCount: a.length, page: 1, pageSize: 50);

void main() {
  late _MockProducts products;
  late _MockCategories categories;
  late _MockPricing pricing;
  late _MockOutlets outlets;
  late _MockClearOutlets clearOutlets;
  late _MockStock stock;
  late _MockAssignments assignments;
  late _MockBillSync billSync;
  late _MockNotBillingSync notBillingSync;
  late List<String> log;
  late BackgroundSyncService service;

  setUpAll(() {
    registerFallbackValue(DateTime(2000));
  });

  setUp(() {
    products = _MockProducts();
    categories = _MockCategories();
    pricing = _MockPricing();
    outlets = _MockOutlets();
    clearOutlets = _MockClearOutlets();
    stock = _MockStock();
    assignments = _MockAssignments();
    billSync = _MockBillSync();
    notBillingSync = _MockNotBillingSync();
    log = [];

    when(() => products(force: any(named: 'force'))).thenAnswer((_) async {
      log.add('products');
      return (const <Never>[], DateTime(2026));
    });
    when(() => categories(force: any(named: 'force'))).thenAnswer((_) async {
      log.add('categories');
      return (const <Never>[], DateTime(2026));
    });
    when(() => pricing(force: any(named: 'force'))).thenAnswer((_) async {
      log.add('pricing');
      return (const <Never>[], DateTime(2026));
    });
    when(() => assignments(date: any(named: 'date'))).thenAnswer((_) async {
      log.add('assignment');
      return _result([_assignment]);
    });
    when(() => outlets(any(), any())).thenAnswer((_) async {
      log.add('outlets');
      return (outlets: const <Never>[], policy: _MockPolicy());
    });
    when(() => clearOutlets()).thenAnswer((_) async => log.add('clearOutlets'));
    when(() => billSync.flushAll(force: any(named: 'force')))
        .thenAnswer((_) async => log.add('bills'));
    when(() => notBillingSync.flushAll(force: any(named: 'force')))
        .thenAnswer((_) async => log.add('notBillings'));
    when(() => stock(force: any(named: 'force')))
        .thenAnswer((_) async => log.add('stock'));

    service = BackgroundSyncService(
      syncProducts: products,
      syncCategories: categories,
      syncPricing: pricing,
      syncOutlets: outlets,
      clearDailyOutlets: clearOutlets,
      syncStock: stock,
      getAssignments: assignments,
      billSync: billSync,
      notBillingSync: notBillingSync,
      flushLocationPings: () async => log.add('pings'),
    );
  });

  test('runs every step; stock comes after the bill flush', () async {
    expect(await service.runSync(), isTrue);

    expect(log.toSet(), {
      'products',
      'categories',
      'pricing',
      'assignment',
      'outlets',
      'bills',
      'notBillings',
      'stock',
      'pings',
    });
    expect(log.indexOf('stock'), greaterThan(log.indexOf('bills')));
    expect(log.indexOf('outlets'), greaterThan(log.indexOf('assignment')));
    // Uploads only start once every download has settled.
    for (final download in ['products', 'categories', 'pricing', 'outlets']) {
      expect(log.indexOf('bills'), greaterThan(log.indexOf(download)));
    }
    verify(() => stock(force: false)).called(1);
    expect(service.progress.value.isSyncing, isFalse);
    expect(service.progress.value.completedAt, isNotNull);
  });

  test('downloads run in parallel, not one after another', () async {
    final productsGate = Completer<void>();
    when(() => products(force: any(named: 'force'))).thenAnswer((_) async {
      log.add('products:start');
      await productsGate.future;
      log.add('products:end');
      return (const <Never>[], DateTime(2026));
    });

    final run = service.runSync();
    // Let the other downloads proceed while products is still in flight.
    await pumpEventQueue();
    expect(log, containsAll(['categories', 'pricing', 'assignment', 'outlets']));
    expect(log, isNot(contains('products:end')));
    expect(log, isNot(contains('bills')));

    productsGate.complete();
    await run;
    expect(log.indexOf('bills'), greaterThan(log.indexOf('products:end')));
  });

  test('a failing download does not stop the other steps', () async {
    when(() => products(force: any(named: 'force')))
        .thenThrow(Exception('products 500'));
    when(() => categories(force: any(named: 'force')))
        .thenAnswer((_) async => throw Exception('categories offline'));

    expect(await service.runSync(), isTrue);

    expect(log, containsAll(
        ['assignment', 'outlets', 'bills', 'notBillings', 'stock', 'pings']));
  });

  test('a failing pricing download does not stop the other steps', () async {
    when(() => pricing(force: any(named: 'force')))
        .thenThrow(Exception('pricing 500'));

    expect(await service.runSync(), isTrue);

    expect(log, containsAll([
      'products',
      'categories',
      'outlets',
      'bills',
      'notBillings',
      'stock',
    ]));
  });

  test('assignment failure skips only outlets, as before', () async {
    when(() => assignments(date: any(named: 'date')))
        .thenAnswer((_) async => throw Exception('assignment 500'));

    expect(await service.runSync(), isTrue);

    verifyNever(() => outlets(any(), any()));
    verifyNever(() => clearOutlets());
    expect(log, containsAll(
        ['products', 'categories', 'bills', 'notBillings', 'stock', 'pings']));
  });

  test('no assignment today clears outlets instead of syncing them', () async {
    when(() => assignments(date: any(named: 'date')))
        .thenAnswer((_) async => _result([]));

    await service.runSync();

    verifyNever(() => outlets(any(), any()));
    verify(() => clearOutlets()).called(1);
  });

  test('bill flush and stock failures stay isolated', () async {
    when(() => billSync.flushAll(force: any(named: 'force')))
        .thenAnswer((_) async => throw Exception('flush'));
    when(() => stock(force: any(named: 'force')))
        .thenAnswer((_) async => throw Exception('stock'));

    expect(await service.runSync(), isTrue);

    expect(log, containsAll(['notBillings', 'pings']));
  });
}
