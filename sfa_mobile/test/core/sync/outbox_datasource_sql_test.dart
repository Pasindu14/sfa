// The suite has no real SQLite (sqflite_common_ffi is not a dev dependency), so
// the outbox's guarded UPDATEs are checked on a recording fake of the sqflite
// executor: the SQL text that would run, its bound arguments, and how the
// datasource reads the changed-row count. The guards are what keep two senders
// from both claiming a row and a late failure report from downgrading a
// synced/cancelled one.
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/not_billings/data/datasources/not_billings_local_datasource.dart';

class _Update {
  final String sql;
  final List<Object?> args;
  const _Update(this.sql, this.args);
}

class _RecordingDb extends Fake implements Database {
  final updates = <_Update>[];
  int changed = 1;

  @override
  Future<int> rawUpdate(String sql, [List<Object?>? arguments]) async {
    updates.add(_Update(sql, arguments ?? const []));
    return changed;
  }
}

class _MockHelper extends Mock implements DatabaseHelper {}

String _flat(String sql) => sql.replaceAll(RegExp(r'\s+'), ' ').trim();

final _at = DateTime.utc(2026, 10, 1, 8, 30);

void main() {
  late _RecordingDb db;
  late _MockHelper helper;

  setUp(() {
    db = _RecordingDb();
    helper = _MockHelper();
    when(() => helper.database).thenAnswer((_) async => db);
  });

  group('bills', () {
    late BillsLocalDatasource local;
    setUp(() => local = BillsLocalDatasource(helper));

    test('claimForSync only moves pending/failed rows and stamps the claim',
        () async {
      final won = await local.claimForSync('b1', now: _at);

      expect(won, isTrue);
      final sql = _flat(db.updates.single.sql);
      expect(sql, contains("SET sync_status = ?, last_attempt_at = ?"));
      expect(sql, contains("AND sync_status IN ('pending', 'failed')"));
      expect(db.updates.single.args,
          ['syncing', '2026-10-01T08:30:00.000Z', 'b1']);
    });

    test('claimForSync loses when no row changed (already claimed)', () async {
      db.changed = 0;

      expect(await local.claimForSync('b1', now: _at), isFalse);
    });

    test('resetStaleSyncing targets syncing rows with a stale or missing stamp',
        () async {
      db.changed = 3;

      final reset = await local.resetStaleSyncing(staleBefore: _at);

      expect(reset, 3);
      final sql = _flat(db.updates.single.sql);
      expect(sql, contains('WHERE sync_status = ?'));
      expect(sql,
          contains('(last_attempt_at IS NULL OR last_attempt_at < ?)'));
      expect(sql, isNot(contains('NOT IN')));
      expect(db.updates.single.args,
          ['pending', 'syncing', '2026-10-01T08:30:00.000Z']);
    });

    test('resetStaleSyncing never touches rows this isolate is sending',
        () async {
      await local.resetStaleSyncing(
          staleBefore: _at, inFlightIds: {'b1', 'b2'});

      final sql = _flat(db.updates.single.sql);
      expect(sql, contains('AND client_bill_id NOT IN (?,?)'));
      expect(db.updates.single.args,
          ['pending', 'syncing', '2026-10-01T08:30:00.000Z', 'b1', 'b2']);
    });

    test('markFailed cannot downgrade a synced or cancelled row', () async {
      await local.markFailed('b1', errorCode: 'X', errorMessage: 'boom');

      expect(_flat(db.updates.single.sql),
          contains("AND sync_status NOT IN ('synced', 'cancelled')"));
    });

    test('markPendingAfterNetworkError cannot downgrade a synced or '
        'cancelled row', () async {
      await local.markPendingAfterNetworkError('b1');

      expect(_flat(db.updates.single.sql),
          contains("AND sync_status NOT IN ('synced', 'cancelled')"));
    });
  });

  group('not_billings', () {
    late NotBillingsLocalDatasource local;
    setUp(() => local = NotBillingsLocalDatasource(helper));

    test('claimForSync only moves pending/failed rows and stamps the claim',
        () async {
      final won = await local.claimForSync('n1', now: _at);

      expect(won, isTrue);
      final sql = _flat(db.updates.single.sql);
      expect(sql, contains('UPDATE not_billings'));
      expect(sql, contains("AND sync_status IN ('pending', 'failed')"));
      expect(db.updates.single.args,
          ['syncing', '2026-10-01T08:30:00.000Z', 'n1']);
    });

    test('claimForSync loses when no row changed (already claimed)', () async {
      db.changed = 0;

      expect(await local.claimForSync('n1', now: _at), isFalse);
    });

    test('resetStaleSyncing skips in-flight ids and fresh claims', () async {
      await local.resetStaleSyncing(staleBefore: _at, inFlightIds: {'n1'});

      final sql = _flat(db.updates.single.sql);
      expect(sql, contains('UPDATE not_billings'));
      expect(sql,
          contains('(last_attempt_at IS NULL OR last_attempt_at < ?)'));
      expect(sql, contains('AND client_not_billing_id NOT IN (?)'));
      expect(db.updates.single.args,
          ['pending', 'syncing', '2026-10-01T08:30:00.000Z', 'n1']);
    });

    test('markFailed and markPendingAfterNetworkError cannot downgrade a '
        'synced row', () async {
      await local.markFailed('n1', errorCode: 'X', errorMessage: 'boom');
      await local.markPendingAfterNetworkError('n1');

      expect(db.updates, hasLength(2));
      for (final u in db.updates) {
        expect(_flat(u.sql),
            contains("AND sync_status NOT IN ('synced', 'cancelled')"));
      }
    });
  });
}
