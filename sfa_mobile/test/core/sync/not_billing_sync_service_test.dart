import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/connectivity/connectivity_service.dart';
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
  });

  setUp(() {
    local = _MockLocal();
    remote = _MockRemote();
    final connectivity = _MockConnectivity();
    when(() => connectivity.onConnectionRestored)
        .thenAnswer((_) => const Stream.empty());
    when(() => local.markSyncing(any())).thenAnswer((_) async {});
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
}
