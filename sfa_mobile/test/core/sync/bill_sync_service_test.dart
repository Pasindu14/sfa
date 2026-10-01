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
    registerFallbackValue(<String>{});
  });

  setUp(() {
    local = _MockLocal();
    remote = _MockRemote();
    connectivity = _MockConnectivity();
    stock = _MockStock();
    outlets = _MockOutlets();

    when(() => connectivity.onConnectionRestored)
        .thenAnswer((_) => const Stream.empty());
    when(() => local.claimForSync(any(), now: any(named: 'now')))
        .thenAnswer((_) async => true);
    when(() => local.resetStaleSyncing(
            staleBefore: any(named: 'staleBefore'),
            inFlightIds: any(named: 'inFlightIds')))
        .thenAnswer((_) async => 0);
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

  group('stuck syncing rows', () {
    test('every flush resets stale rows before it reads the batch', () async {
      when(() => local.getPendingForSync()).thenAnswer((_) async => []);

      await service.flushAll();

      verifyInOrder([
        () => local.resetStaleSyncing(
              staleBefore: _now.subtract(BillSyncService.syncingStaleAfter),
              inFlightIds: <String>{},
            ),
        () => local.getPendingForSync(),
      ]);
    });

    test('a row this isolate is sending is excluded from the reset', () async {
      final gate = Completer<CreateBillingResponse>();
      when(() => local.getPendingForSync())
          .thenAnswer((_) async => [_bill('b1')]);
      when(() => remote.createBilling(any())).thenAnswer((_) => gate.future);

      final flush = service.flushAll();
      await pumpEventQueue();
      await service.recoverStuckRows();
      gate.complete(_ok);
      await flush;

      // The first call (flush start) saw nothing in flight; the second, made
      // while b1's POST was pending, must name it.
      final seen = verify(() => local.resetStaleSyncing(
            staleBefore: any(named: 'staleBefore'),
            inFlightIds: captureAny(named: 'inFlightIds'),
          )).captured;
      expect(seen.first, isEmpty);
      expect(seen[1], {'b1'});
    });

    test('rows that were reset are announced so the badge picks them up',
        () async {
      when(() => local.resetStaleSyncing(
            staleBefore: any(named: 'staleBefore'),
            inFlightIds: any(named: 'inFlightIds'),
          )).thenAnswer((_) async => 2);
      when(() => local.countPendingOrFailed()).thenAnswer((_) async => 2);
      final events = <BillOutboxStatus>[];
      final sub = service.status$.listen(events.add);

      final reset = await service.recoverStuckRows();
      await pumpEventQueue();
      await sub.cancel();

      expect(reset, 2);
      expect(events.single.pendingOrFailedCount, 2);
    });

    test('a failing reset never blocks the flush', () async {
      when(() => local.resetStaleSyncing(
            staleBefore: any(named: 'staleBefore'),
            inFlightIds: any(named: 'inFlightIds'),
          )).thenThrow(StateError('db locked'));
      when(() => local.getPendingForSync())
          .thenAnswer((_) async => [_bill('b1')]);

      await service.flushAll();

      verify(() => remote.createBilling(any())).called(1);
    });

    test('manual retry on a stale syncing row recovers it and sends', () async {
      final stale = _bill('b1',
          status: SyncStatus.syncing,
          lastAttemptAt: _now.subtract(const Duration(minutes: 5)));
      var reads = 0;
      when(() => local.getById('b1')).thenAnswer((_) async {
        reads++;
        return reads == 1 ? stale : _bill('b1', attempts: 0);
      });
      when(() => local.resetStaleSyncing(
            staleBefore: any(named: 'staleBefore'),
            inFlightIds: any(named: 'inFlightIds'),
          )).thenAnswer((_) async => 1);

      await service.flushOne('b1');

      verify(() => remote.createBilling(any())).called(1);
    });

    test('manual retry on a syncing row that is still live does nothing',
        () async {
      when(() => local.getById('b1')).thenAnswer((_) async => _bill('b1',
          status: SyncStatus.syncing,
          lastAttemptAt: _now.subtract(const Duration(seconds: 10))));

      await service.flushOne('b1');

      verifyNever(() => remote.createBilling(any()));
    });
  });

  group('atomic claim', () {
    test('a lost claim sends nothing and records no failure', () async {
      when(() => local.claimForSync(any(), now: any(named: 'now')))
          .thenAnswer((_) async => false);
      when(() => local.getById('b1')).thenAnswer((_) async => _bill('b1'));

      await service.flushOne('b1');

      verifyNever(() => remote.createBilling(any()));
      verifyNever(() => local.markPendingAfterNetworkError(any()));
      verifyNever(() => stock(force: any(named: 'force')));
    });

    test('two senders racing for one row: the second claimer loses', () async {
      // Two services over one store model the UI isolate and the WorkManager
      // isolate: separate _inFlight sets, one shared database row.
      final claimed = <String>{};
      when(() => local.claimForSync(any(), now: any(named: 'now')))
          .thenAnswer((inv) async => claimed.add(inv.positionalArguments.first as String));
      when(() => local.getById('b1')).thenAnswer((_) async => _bill('b1'));
      final gate = Completer<CreateBillingResponse>();
      when(() => remote.createBilling(any())).thenAnswer((_) => gate.future);
      final other = BillSyncService(local, remote, connectivity, stock, outlets,
          clock: () => _now);
      addTearDown(other.dispose);

      final first = service.flushOne('b1');
      final second = other.flushOne('b1');
      await pumpEventQueue();
      gate.complete(_ok);
      await Future.wait([first, second]);

      verify(() => remote.createBilling(any())).called(1);
    });

    test('a synced or cancelled row is never claimed again', () async {
      for (final status in [SyncStatus.synced, SyncStatus.cancelled]) {
        when(() => local.getById('b1'))
            .thenAnswer((_) async => _bill('b1', status: status));

        await service.flushOne('b1');
      }

      verifyNever(() => local.claimForSync(any(), now: any(named: 'now')));
      verifyNever(() => remote.createBilling(any()));
    });

    test('claim time comes from the injected clock', () async {
      when(() => local.getById('b1')).thenAnswer((_) async => _bill('b1'));

      await service.flushOne('b1');

      verify(() => local.claimForSync('b1', now: _now)).called(1);
    });
  });

  group('delete flow', () {
    const found = ServerBillLookup(
        id: 77,
        billingNumber: 'B-77',
        repStatus: 'Submitted',
        distributorStatus: 'Pending');

    setUp(() {
      when(() => local.markCancelled(any())).thenAnswer((_) async {});
      when(() => remote.cancelBilling(any())).thenAnswer((_) async {});
      when(() => remote.findByClientBillId(any()))
          .thenAnswer((_) async => found);
    });

    void stubRow(BillModel row) =>
        when(() => local.getById(row.clientBillId)).thenAnswer((_) async => row);

    test('a bill that was never attempted is cancelled locally, no lookup',
        () async {
      stubRow(_bill('b1'));

      await service.cancelOne('b1');

      verifyNever(() => remote.findByClientBillId(any()));
      verify(() => local.markCancelled('b1')).called(1);
    });

    test('attempted bill the server holds: cancelled there with the found id',
        () async {
      stubRow(_bill('b1', attempts: 1, lastAttemptAt: _now));

      await service.cancelOne('b1');

      verifyInOrder([
        () => remote.findByClientBillId('b1'),
        () => remote.cancelBilling(77),
        () => local.markCancelled('b1'),
      ]);
    });

    test('a bill the server already has cancelled is not cancelled twice',
        () async {
      when(() => remote.findByClientBillId(any())).thenAnswer((_) async =>
          const ServerBillLookup(id: 77, repStatus: 'Cancelled'));
      stubRow(_bill('b1', attempts: 2, lastAttemptAt: _now));

      await service.cancelOne('b1');

      verifyNever(() => remote.cancelBilling(any()));
      verify(() => local.markCancelled('b1')).called(1);
    });

    test('server 404: it never arrived, so cancel locally only', () async {
      when(() => remote.findByClientBillId(any()))
          .thenAnswer((_) async => null);
      stubRow(_bill('b1', attempts: 1, lastAttemptAt: _now));

      await service.cancelOne('b1');

      verifyNever(() => remote.cancelBilling(any()));
      verify(() => local.markCancelled('b1')).called(1);
    });

    test('a syncing row counts as attempted even with zero recorded attempts',
        () async {
      stubRow(_bill('b1',
          status: SyncStatus.syncing,
          lastAttemptAt: _now.subtract(const Duration(minutes: 10))));

      await service.cancelOne('b1');

      verify(() => remote.findByClientBillId('b1')).called(1);
      verify(() => local.markCancelled('b1')).called(1);
    });

    test('lookup network error: refuses with the connect-to-internet message',
        () async {
      when(() => remote.findByClientBillId(any()))
          .thenThrow(const NetworkException(message: 'offline'));
      stubRow(_bill('b1', attempts: 1, lastAttemptAt: _now));

      await expectLater(
        service.cancelOne('b1'),
        throwsA(isA<NetworkException>().having((e) => e.message, 'message',
            'Connect to the internet to delete this bill.')),
      );

      verifyNever(() => local.markCancelled(any()));
    });

    test('lookup rejected by the server: refuses, row untouched', () async {
      when(() => remote.findByClientBillId(any())).thenThrow(
          const ServerException(code: 'HTTP_404', message: 'no envelope'));
      stubRow(_bill('b1', attempts: 1, lastAttemptAt: _now));

      await expectLater(
          service.cancelOne('b1'), throwsA(isA<BusinessRuleException>()));

      verifyNever(() => local.markCancelled(any()));
    });

    test('cancel network error after a successful lookup: refuses', () async {
      when(() => remote.cancelBilling(any()))
          .thenThrow(const NetworkException(message: 'offline'));
      stubRow(_bill('b1', attempts: 1, lastAttemptAt: _now));

      await expectLater(
          service.cancelOne('b1'), throwsA(isA<NetworkException>()));

      verifyNever(() => local.markCancelled(any()));
    });

    test('server refuses the cancel: the bill is adopted as synced, not hidden',
        () async {
      when(() => remote.cancelBilling(any())).thenThrow(
          const BusinessRuleException(
              code: 'INVALID_BILLING_STATE', message: 'already approved'));
      stubRow(_bill('b1', attempts: 1, lastAttemptAt: _now));

      await expectLater(
        service.cancelOne('b1'),
        throwsA(isA<BusinessRuleException>()
            .having((e) => e.code, 'code', 'BILL_ALREADY_RECEIVED')),
      );

      verify(() => local.markSynced('b1',
          serverBillId: 77, serverBillNumber: 'B-77')).called(1);
      verifyNever(() => local.markCancelled(any()));
    });

    test('refused while this isolate is sending the row', () async {
      final gate = Completer<CreateBillingResponse>();
      when(() => remote.createBilling(any())).thenAnswer((_) => gate.future);
      stubRow(_bill('b1'));

      final send = service.flushOne('b1');
      await pumpEventQueue();
      await expectLater(
        service.cancelOne('b1'),
        throwsA(isA<BusinessRuleException>()
            .having((e) => e.code, 'code', 'BILL_SYNC_IN_PROGRESS')),
      );
      gate.complete(_ok);
      await send;

      verifyNever(() => remote.findByClientBillId(any()));
      verifyNever(() => local.markCancelled(any()));
    });

    test('refused while another isolate holds a fresh claim', () async {
      stubRow(_bill('b1',
          status: SyncStatus.syncing,
          lastAttemptAt: _now.subtract(const Duration(seconds: 20))));

      await expectLater(
          service.cancelOne('b1'), throwsA(isA<BusinessRuleException>()));

      verifyNever(() => remote.findByClientBillId(any()));
      verifyNever(() => local.markCancelled(any()));
    });

    test('a flush cannot send a row while its delete is asking the server',
        () async {
      final gate = Completer<ServerBillLookup?>();
      when(() => remote.findByClientBillId(any())).thenAnswer((_) => gate.future);
      stubRow(_bill('b1', attempts: 1, lastAttemptAt: _now));

      final delete = service.cancelOne('b1');
      await pumpEventQueue();
      await service.flushOne('b1');
      gate.complete(null);
      await delete;

      verifyNever(() => remote.createBilling(any()));
      verify(() => local.markCancelled('b1')).called(1);
    });

    test('a synced bill is cancelled on the server, errors swallowed',
        () async {
      when(() => remote.cancelBilling(any()))
          .thenThrow(const NetworkException(message: 'offline'));
      stubRow(BillModel(
        clientBillId: 'b1',
        outletId: 1,
        billingDate: _now,
        billDiscountRate: 0,
        subTotalAmount: 100,
        billDiscountAmount: 0,
        totalAmount: 100,
        createdAt: _now,
        syncStatus: SyncStatus.synced,
        serverBillId: 5,
      ));

      await service.cancelOne('b1');

      verify(() => remote.cancelBilling(5)).called(1);
      verifyNever(() => remote.findByClientBillId(any()));
      verify(() => local.markCancelled('b1')).called(1);
    });
  });
}
