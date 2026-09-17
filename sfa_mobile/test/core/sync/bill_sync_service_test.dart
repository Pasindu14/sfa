// Guards the bill outbox against the flush storms seen when a rep comes back
// online with a queue: overlapping triggers re-POSTing the same bill, a full
// stock download per synced bill, and failed rows retrying on every trigger.
import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/connectivity/connectivity_service.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/data/datasources/bills_remote_datasource.dart';
import 'package:uswatte/features/bills/data/models/bill_model.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/outlets/data/datasources/outlets_local_datasource.dart';
import 'package:uswatte/features/stock/domain/usecases/sync_distributor_stock_usecase.dart';

class _MockLocal extends Mock implements BillsLocalDatasource {}

class _MockRemote extends Mock implements BillsRemoteDatasource {}

class _MockConnectivity extends Mock implements ConnectivityService {}

class _MockStock extends Mock implements SyncDistributorStockUseCase {}

class _MockOutlets extends Mock implements OutletsLocalDatasource {}

final _now = DateTime.utc(2026, 9, 17, 10);

const _ok = CreateBillingResponse(serverBillId: 1, serverBillNumber: 'B-1');

BillModel _bill(
  String id, {
  SyncStatus status = SyncStatus.pending,
  int attempts = 0,
  DateTime? lastAttemptAt,
  String? errorCode,
}) =>
    BillModel(
      clientBillId: id,
      outletId: 1,
      billingDate: _now,
      billDiscountRate: 0,
      subTotalAmount: 100,
      billDiscountAmount: 0,
      totalAmount: 100,
      createdAt: _now,
      syncStatus: status,
      syncAttempts: attempts,
      lastAttemptAt: lastAttemptAt,
      lastSyncErrorCode: errorCode,
    );

String _idOf(Invocation inv) =>
    (inv.positionalArguments.first as BillModel).clientBillId;

void main() {
  late _MockLocal local;
  late _MockRemote remote;
  late _MockConnectivity connectivity;
  late _MockStock stock;
  late _MockOutlets outlets;
  late BillSyncService service;

  setUpAll(() {
    registerFallbackValue(_bill('fallback'));
    registerFallbackValue(DateTime(2000));
  });

  setUp(() {
    local = _MockLocal();
    remote = _MockRemote();
    connectivity = _MockConnectivity();
    stock = _MockStock();
    outlets = _MockOutlets();

    when(() => connectivity.onConnectionRestored)
        .thenAnswer((_) => const Stream.empty());
    when(() => local.markSyncing(any())).thenAnswer((_) async {});
    when(() => local.countPendingOrFailed()).thenAnswer((_) async => 0);
    when(() => local.markSynced(any(),
            serverBillId: any(named: 'serverBillId'),
            serverBillNumber: any(named: 'serverBillNumber')))
        .thenAnswer((_) async {});
    when(() => local.markPendingAfterNetworkError(any()))
        .thenAnswer((_) async {});
    when(() => local.purgeSyncedOlderThan(any())).thenAnswer((_) async => 0);
    when(() => outlets.stampLastBillDate(any(), any()))
        .thenAnswer((_) async {});
    when(() => stock(force: any(named: 'force'))).thenAnswer((_) async {});
    when(() => remote.createBilling(any())).thenAnswer((_) async => _ok);

    service = BillSyncService(local, remote, connectivity, stock, outlets,
        clock: () => _now);
  });

  tearDown(() => service.dispose());

  group('stock refresh', () {
    test('flushAll syncs stock once for many bills', () async {
      when(() => local.getPendingForSync())
          .thenAnswer((_) async => List.generate(15, (i) => _bill('b$i')));

      await service.flushAll();

      verify(() => remote.createBilling(any())).called(15);
      verify(() => stock(force: true)).called(1);
    });

    test('flushAll skips stock when nothing synced', () async {
      when(() => local.getPendingForSync())
          .thenAnswer((_) async => [_bill('b1')]);
      when(() => remote.createBilling(any()))
          .thenThrow(const NetworkException(message: 'offline'));

      await service.flushAll();

      verifyNever(() => stock(force: any(named: 'force')));
    });

    test('flushOne still syncs stock after success', () async {
      when(() => local.getById('b1')).thenAnswer((_) async => _bill('b1'));

      await service.flushOne('b1');

      verify(() => stock(force: true)).called(1);
    });
  });

  group('single-flight', () {
    test('overlapping flushAll calls POST each bill once', () async {
      final gate = Completer<CreateBillingResponse>();
      when(() => local.getPendingForSync())
          .thenAnswer((_) async => [_bill('b1'), _bill('b2')]);
      final sent = <String>[];
      when(() => remote.createBilling(any())).thenAnswer((inv) {
        final id = _idOf(inv);
        sent.add(id);
        return id == 'b1' ? gate.future : Future.value(_ok);
      });

      final first = service.flushAll();
      final second = service.flushAll();
      final third = service.flushAll();
      expect(service.isFlushing, isTrue);

      await pumpEventQueue();
      gate.complete(_ok);
      await Future.wait([first, second, third]);

      verify(() => local.getPendingForSync()).called(1);
      expect(sent, ['b1', 'b2']);
      expect(service.isFlushing, isFalse);
    });

    test('flushAll skips a row flushOne already sent from a stale batch',
        () async {
      final gate = Completer<CreateBillingResponse>();
      when(() => local.getById('b2')).thenAnswer((_) async => _bill('b2'));
      when(() => local.getPendingForSync())
          .thenAnswer((_) async => [_bill('b1'), _bill('b2')]);
      final sent = <String>[];
      when(() => remote.createBilling(any())).thenAnswer((inv) {
        final id = _idOf(inv);
        sent.add(id);
        return id == 'b1' ? gate.future : Future.value(_ok);
      });

      final flush = service.flushAll();
      await pumpEventQueue();
      // b1 is blocked mid-POST; the rep submits/retries b2 meanwhile.
      await service.flushOne('b2');
      gate.complete(_ok);
      await flush;

      expect(sent, ['b1', 'b2']);
    });

    test('a forced flush requested mid-flush runs its own pass after', () async {
      final gate = Completer<CreateBillingResponse>();
      final backedOff = _bill('b2',
          status: SyncStatus.failed,
          attempts: 3,
          lastAttemptAt: _now,
          errorCode: 'INTERNAL_ERROR');
      var reads = 0;
      when(() => local.getPendingForSync()).thenAnswer((_) async {
        reads++;
        return reads == 1 ? [_bill('b1'), backedOff] : [backedOff];
      });
      final sent = <String>[];
      when(() => remote.createBilling(any())).thenAnswer((inv) {
        final id = _idOf(inv);
        sent.add(id);
        return id == 'b1' ? gate.future : Future.value(_ok);
      });

      final auto = service.flushAll();
      final manual = service.flushAll(force: true);
      await pumpEventQueue();
      gate.complete(_ok);
      await Future.wait([auto, manual]);

      expect(sent, ['b1', 'b2']);
    });
  });

  group('backoff', () {
    test('failed row inside its backoff window is skipped', () async {
      when(() => local.getPendingForSync()).thenAnswer((_) async => [
            _bill('b1',
                status: SyncStatus.failed,
                attempts: 2, // 60s delay
                lastAttemptAt: _now.subtract(const Duration(seconds: 30)),
                errorCode: 'INTERNAL_ERROR'),
          ]);

      await service.flushAll();

      verifyNever(() => remote.createBilling(any()));
    });

    test('failed row past its backoff window is retried', () async {
      when(() => local.getPendingForSync()).thenAnswer((_) async => [
            _bill('b1',
                status: SyncStatus.failed,
                attempts: 2,
                lastAttemptAt: _now.subtract(const Duration(seconds: 61)),
                errorCode: 'INTERNAL_ERROR'),
          ]);

      await service.flushAll();

      verify(() => remote.createBilling(any())).called(1);
    });

    test('force bypasses backoff but not terminal errors', () async {
      when(() => local.getPendingForSync()).thenAnswer((_) async => [
            _bill('b1',
                status: SyncStatus.failed,
                attempts: 5,
                lastAttemptAt: _now,
                errorCode: 'INTERNAL_ERROR'),
            _bill('b2',
                status: SyncStatus.failed,
                attempts: 1,
                lastAttemptAt: _now.subtract(const Duration(hours: 1)),
                errorCode: 'INSUFFICIENT_STOCK'),
          ]);

      await service.flushAll(force: true);

      final sent = verify(() => remote.createBilling(captureAny())).captured;
      expect(sent.map((b) => (b as BillModel).clientBillId), ['b1']);
    });

    test('pending rows are not delayed', () async {
      when(() => local.getPendingForSync()).thenAnswer(
          (_) async => [_bill('b1', attempts: 4, lastAttemptAt: _now)]);

      await service.flushAll();

      verify(() => remote.createBilling(any())).called(1);
    });
  });
}
