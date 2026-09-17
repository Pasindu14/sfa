import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/features/stock/data/datasources/distributor_stock_local_datasource.dart';
import 'package:uswatte/features/stock/data/datasources/distributor_stock_remote_datasource.dart';
import 'package:uswatte/features/stock/data/models/distributor_stock_model.dart';
import 'package:uswatte/features/stock/domain/usecases/sync_distributor_stock_usecase.dart';

class _MockRemote extends Mock implements DistributorStockRemoteDatasource {}

class _MockLocal extends Mock implements DistributorStockLocalDatasource {}

void main() {
  late _MockRemote remote;
  late _MockLocal local;
  late DateTime now;
  late SyncDistributorStockUseCase useCase;

  setUpAll(() {
    registerFallbackValue(DateTime(2000));
    registerFallbackValue(<DistributorStockModel>[]);
  });

  setUp(() {
    remote = _MockRemote();
    local = _MockLocal();
    now = DateTime(2026, 9, 17, 10);
    when(() => remote.fetchAll()).thenAnswer((_) async => []);
    when(() => local.replaceAll(any())).thenAnswer((_) async {});
    when(() => local.saveLastSyncedAt(any())).thenAnswer((_) async {});
    useCase = SyncDistributorStockUseCase(remote, local, clock: () => now);
  });

  test('concurrent calls share one download', () async {
    final gate = Completer<List<DistributorStockModel>>();
    when(() => remote.fetchAll()).thenAnswer((_) => gate.future);

    final a = useCase();
    final b = useCase();
    gate.complete([]);
    await Future.wait([a, b]);

    verify(() => remote.fetchAll()).called(1);
  });

  test('a call within a minute of a successful sync is skipped', () async {
    await useCase();
    now = now.add(const Duration(seconds: 59));
    await useCase();
    verify(() => remote.fetchAll()).called(1);

    now = now.add(const Duration(seconds: 2));
    await useCase();
    verify(() => remote.fetchAll()).called(1);
  });

  test('force ignores the throttle', () async {
    await useCase();
    await useCase(force: true);
    verify(() => remote.fetchAll()).called(2);
  });

  test('a failed sync does not start the throttle', () async {
    when(() => remote.fetchAll()).thenThrow(Exception('boom'));
    await expectLater(useCase(), throwsException);

    when(() => remote.fetchAll()).thenAnswer((_) async => []);
    await useCase();
    verify(() => remote.fetchAll()).called(2);
  });

  test('force during an in-flight download fetches again after it', () async {
    final gate = Completer<List<DistributorStockModel>>();
    var calls = 0;
    when(() => remote.fetchAll()).thenAnswer((_) {
      calls++;
      return calls == 1 ? gate.future : Future.value([]);
    });

    final a = useCase();
    final b = useCase(force: true);
    gate.complete([]);
    await Future.wait([a, b]);

    expect(calls, 2);
  });
}
