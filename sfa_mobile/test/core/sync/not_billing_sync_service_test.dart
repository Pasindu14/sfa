import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/connectivity/connectivity_service.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/sync/not_billing_sync_service.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/not_billings/data/datasources/not_billings_local_datasource.dart';
import 'package:uswatte/features/not_billings/data/datasources/not_billings_remote_datasource.dart';
import 'package:uswatte/features/not_billings/data/models/not_billing_model.dart';
import 'package:uswatte/features/not_billings/domain/entities/not_billing_reason.dart';

class _MockLocal extends Mock implements NotBillingsLocalDatasource {}

class _MockRemote extends Mock implements NotBillingsRemoteDatasource {}

class _MockConnectivity extends Mock implements ConnectivityService {}

final _now = DateTime.utc(2026, 9, 17, 10);

NotBillingModel _record(
  String id, {
  SyncStatus status = SyncStatus.pending,
  int attempts = 0,
  DateTime? lastAttemptAt,
  String? errorCode,
}) =>
    NotBillingModel(
      clientNotBillingId: id,
      outletId: 1,
      notBillingDate: _now,
      reason: NotBillingReason.outletClosed,
      createdAt: _now,
      syncStatus: status,
      syncAttempts: attempts,
      lastAttemptAt: lastAttemptAt,
      lastSyncErrorCode: errorCode,
    );

void main() {
  late _MockLocal local;
  late _MockRemote remote;
  late NotBillingSyncService service;

  setUpAll(() {
    registerFallbackValue(_record('fallback'));
    registerFallbackValue(DateTime(2000));
    registerFallbackValue(<String>{});
  });

  setUp(() {
    local = _MockLocal();
    remote = _MockRemote();
    final connectivity = _MockConnectivity();
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
            serverNotBillingId: any(named: 'serverNotBillingId'),
            serverNotBillingNumber: any(named: 'serverNotBillingNumber')))
        .thenAnswer((_) async {});
    when(() => local.purgeSyncedOlderThan(any())).thenAnswer((_) async => 0);
    when(() => remote.createNotBilling(any())).thenAnswer((_) async =>
        const CreateNotBillingResponse(
            serverNotBillingId: 1, serverNotBillingNumber: 'NB-1'));

    service =
        NotBillingSyncService(local, remote, connectivity, clock: () => _now);
  });

  tearDown(() => service.dispose());

  test('overlapping flushAll calls share one pass', () async {
    final gate = Completer<List<NotBillingModel>>();
    when(() => local.getPendingForSync()).thenAnswer((_) => gate.future);

    final a = service.flushAll();
    final b = service.flushAll();
    gate.complete([_record('n1')]);
    await Future.wait([a, b]);

    verify(() => local.getPendingForSync()).called(1);
    verify(() => remote.createNotBilling(any())).called(1);
  });

  test('terminal and backed-off rows are skipped', () async {
    when(() => local.getPendingForSync()).thenAnswer((_) async => [
          _record('dup',
              status: SyncStatus.failed,
              attempts: 1,
              errorCode: 'DUPLICATE_NOT_BILLING'),
          _record('invalid',
              status: SyncStatus.failed,
              attempts: 1,
              errorCode: 'VALIDATION_FAILED'),
          _record('waiting',
              status: SyncStatus.failed,
              attempts: 3,
              lastAttemptAt: _now.subtract(const Duration(minutes: 1)),
              errorCode: 'INTERNAL_ERROR'),
          _record('ok'),
        ]);

    await service.flushAll();

    final sent = verify(() => remote.createNotBilling(captureAny())).captured;
    expect(sent.map((r) => (r as NotBillingModel).clientNotBillingId), ['ok']);
  });

  test('flushOne skips a terminal row', () async {
    when(() => local.getById('dup')).thenAnswer((_) async => _record('dup',
        status: SyncStatus.failed, errorCode: 'DUPLICATE_NOT_BILLING'));

    await service.flushOne('dup');

    verifyNever(() => remote.createNotBilling(any()));
  });

  group('stuck syncing rows', () {
    test('every flush resets stale rows before it reads the batch', () async {
      when(() => local.getPendingForSync()).thenAnswer((_) async => []);

      await service.flushAll();

      verifyInOrder([
        () => local.resetStaleSyncing(
              staleBefore:
                  _now.subtract(NotBillingSyncService.syncingStaleAfter),
              inFlightIds: <String>{},
            ),
        () => local.getPendingForSync(),
      ]);
    });

    test('a row this isolate is sending is excluded from the reset', () async {
      final gate = Completer<CreateNotBillingResponse>();
      when(() => local.getPendingForSync())
          .thenAnswer((_) async => [_record('n1')]);
      when(() => remote.createNotBilling(any())).thenAnswer((_) => gate.future);

      final flush = service.flushAll();
      await pumpEventQueue();
      await service.recoverStuckRows();
      gate.complete(const CreateNotBillingResponse(
          serverNotBillingId: 1, serverNotBillingNumber: 'NB-1'));
      await flush;

      final seen = verify(() => local.resetStaleSyncing(
            staleBefore: any(named: 'staleBefore'),
            inFlightIds: captureAny(named: 'inFlightIds'),
          )).captured;
      expect(seen.first, isEmpty);
      expect(seen[1], {'n1'});
    });

    test('reset rows are announced so the badge counts them', () async {
      when(() => local.resetStaleSyncing(
            staleBefore: any(named: 'staleBefore'),
            inFlightIds: any(named: 'inFlightIds'),
          )).thenAnswer((_) async => 1);
      when(() => local.countPendingOrFailed()).thenAnswer((_) async => 1);
      final events = <NotBillingOutboxStatus>[];
      final sub = service.status$.listen(events.add);

      final reset = await service.recoverStuckRows();
      await pumpEventQueue();
      await sub.cancel();

      expect(reset, 1);
      expect(events.single.pendingOrFailedCount, 1);
    });

    test('a failing reset never blocks the flush', () async {
      when(() => local.resetStaleSyncing(
            staleBefore: any(named: 'staleBefore'),
            inFlightIds: any(named: 'inFlightIds'),
          )).thenThrow(StateError('db locked'));
      when(() => local.getPendingForSync())
          .thenAnswer((_) async => [_record('n1')]);

      await service.flushAll();

      verify(() => remote.createNotBilling(any())).called(1);
    });
  });

  group('atomic claim', () {
    test('a lost claim sends nothing and records no failure', () async {
      when(() => local.claimForSync(any(), now: any(named: 'now')))
          .thenAnswer((_) async => false);
      when(() => local.getById('n1')).thenAnswer((_) async => _record('n1'));

      await service.flushOne('n1');

      verifyNever(() => remote.createNotBilling(any()));
      verifyNever(() => local.markPendingAfterNetworkError(any()));
    });

    test('two senders racing for one row: the second claimer loses', () async {
      final claimed = <String>{};
      when(() => local.claimForSync(any(), now: any(named: 'now'))).thenAnswer(
          (inv) async => claimed.add(inv.positionalArguments.first as String));
      when(() => local.getById('n1')).thenAnswer((_) async => _record('n1'));
      final gate = Completer<CreateNotBillingResponse>();
      when(() => remote.createNotBilling(any())).thenAnswer((_) => gate.future);
      final connectivity = _MockConnectivity();
      when(() => connectivity.onConnectionRestored)
          .thenAnswer((_) => const Stream.empty());
      final other =
          NotBillingSyncService(local, remote, connectivity, clock: () => _now);
      addTearDown(other.dispose);

      final first = service.flushOne('n1');
      final second = other.flushOne('n1');
      await pumpEventQueue();
      gate.complete(const CreateNotBillingResponse(
          serverNotBillingId: 1, serverNotBillingNumber: 'NB-1'));
      await Future.wait([first, second]);

      verify(() => remote.createNotBilling(any())).called(1);
    });

    test('a live syncing row is left alone by a manual retry', () async {
      when(() => local.getById('n1')).thenAnswer((_) async => _record('n1',
          status: SyncStatus.syncing,
          lastAttemptAt: _now.subtract(const Duration(seconds: 5))));

      await service.flushOne('n1');

      verifyNever(() => remote.createNotBilling(any()));
    });
  });

  group('delete', () {
    setUp(() {
      when(() => local.delete(any())).thenAnswer((_) async {});
    });

    test('an idle unsynced visit is deleted', () async {
      when(() => local.getById('n1')).thenAnswer((_) async => _record('n1'));

      await service.deleteOne('n1');

      verify(() => local.delete('n1')).called(1);
    });

    test('refused while this isolate is sending it', () async {
      final gate = Completer<CreateNotBillingResponse>();
      when(() => remote.createNotBilling(any())).thenAnswer((_) => gate.future);
      when(() => local.getById('n1')).thenAnswer((_) async => _record('n1'));

      final send = service.flushOne('n1');
      await pumpEventQueue();
      await expectLater(
        service.deleteOne('n1'),
        throwsA(isA<BusinessRuleException>()
            .having((e) => e.code, 'code', 'NOT_BILLING_SYNC_IN_PROGRESS')),
      );
      gate.complete(const CreateNotBillingResponse(
          serverNotBillingId: 1, serverNotBillingNumber: 'NB-1'));
      await send;

      verifyNever(() => local.delete(any()));
    });

    test('refused while another isolate holds a fresh claim', () async {
      when(() => local.getById('n1')).thenAnswer((_) async => _record('n1',
          status: SyncStatus.syncing,
          lastAttemptAt: _now.subtract(const Duration(seconds: 20))));

      await expectLater(
          service.deleteOne('n1'), throwsA(isA<BusinessRuleException>()));

      verifyNever(() => local.delete(any()));
    });

    test('a stale syncing claim is not "being sent"', () async {
      when(() => local.getById('n1')).thenAnswer((_) async => _record('n1',
          status: SyncStatus.syncing,
          lastAttemptAt: _now.subtract(const Duration(minutes: 10))));

      await service.deleteOne('n1');

      verify(() => local.delete('n1')).called(1);
    });
  });
}
