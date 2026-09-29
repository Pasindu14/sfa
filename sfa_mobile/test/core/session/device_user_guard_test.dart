// The suite has no real SQLite (no sqflite_common_ffi), so DeviceUserGuard is
// checked against a recording fake: which tables/rows it clears for a same,
// different, or unknown previous user.
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/core/session/device_user_guard.dart';

class _Log {
  String? lastUserId;
  final deletes = <String>[]; // "table" or "table WHERE ..."
  final writes = <Map<String, Object?>>[];
}

mixin _Recording {
  _Log get log;

  Future<List<Map<String, Object?>>> query(String table,
      {bool? distinct,
      List<String>? columns,
      String? where,
      List<Object?>? whereArgs,
      String? groupBy,
      String? having,
      String? orderBy,
      int? limit,
      int? offset}) async {
    expect(table, 'metadata');
    return log.lastUserId == null
        ? []
        : [
            {'value': log.lastUserId}
          ];
  }

  Future<List<Map<String, Object?>>> rawQuery(String sql,
          [List<Object?>? arguments]) async =>
      [
        {'c': 0}
      ];

  Future<int> delete(String table,
      {String? where, List<Object?>? whereArgs}) async {
    log.deletes.add(where == null ? table : '$table WHERE $where');
    return 0;
  }

  Future<int> rawDelete(String sql, [List<Object?>? arguments]) async {
    log.deletes.add(sql);
    return 0;
  }

  Future<int> insert(String table, Map<String, Object?> values,
      {String? nullColumnHack, ConflictAlgorithm? conflictAlgorithm}) async {
    expect(table, 'metadata');
    log.writes.add(values);
    return 1;
  }
}

class _FakeTxn extends Fake with _Recording implements Transaction {
  @override
  final _Log log;
  _FakeTxn(this.log);
}

class _FakeDb extends Fake with _Recording implements Database {
  @override
  final _Log log;
  _FakeDb(this.log);

  @override
  Future<T> transaction<T>(Future<T> Function(Transaction txn) action,
          {bool? exclusive}) =>
      action(_FakeTxn(log));
}

class _MockHelper extends Mock implements DatabaseHelper {}

void main() {
  late _Log log;
  late DeviceUserGuard guard;

  setUp(() {
    log = _Log();
    final helper = _MockHelper();
    when(() => helper.database).thenAnswer((_) async => _FakeDb(log));
    guard = DeviceUserGuard(helper);
  });

  bool clearedWhole(String table) => log.deletes.contains(table);

  test('same user signing back in keeps everything, including the outbox',
      () async {
    log.lastUserId = '42';
    await guard.onLogin('42');

    expect(log.deletes, isEmpty);
    expect(log.writes, isEmpty);
  });

  test('a different user clears all per-user data and records the new owner',
      () async {
    log.lastUserId = '42';
    await guard.onLogin('99');

    for (final t in [
      'bills',
      'bill_items',
      'not_billings',
      'pending_location_pings',
      'daily_outlets',
      'distributor_stocks',
    ]) {
      expect(clearedWhole(t), isTrue, reason: '$t not cleared');
    }
    // No FK cascade on this DB — children must go first.
    expect(log.deletes.indexOf('bill_items'),
        lessThan(log.deletes.indexOf('bills')));
    expect(log.deletes.any((d) => d.startsWith('metadata WHERE key IN')),
        isTrue);
    expect(log.writes.single,
        {'key': DeviceUserGuard.lastUserIdKey, 'value': '99'});
  });

  test('unknown previous user clears caches but keeps unsynced outbox rows',
      () async {
    await guard.onLogin('7');

    expect(clearedWhole('bills'), isFalse);
    expect(clearedWhole('not_billings'), isFalse);
    expect(clearedWhole('pending_location_pings'), isFalse);
    expect(log.deletes, contains(startsWith('bills WHERE NOT (')));
    expect(log.deletes, contains(startsWith('not_billings WHERE NOT (')));
    expect(clearedWhole('daily_outlets'), isTrue);
    expect(clearedWhole('distributor_stocks'), isTrue);
    expect(log.writes.single['value'], '7');
  });

  test('recordIfUnknown only backfills when nothing is recorded', () async {
    log.lastUserId = '42';
    await guard.recordIfUnknown('42');
    expect(log.writes, isEmpty);

    log.lastUserId = null;
    await guard.recordIfUnknown('42');
    expect(log.writes.single['value'], '42');
    expect(log.deletes, isEmpty);
  });
}
