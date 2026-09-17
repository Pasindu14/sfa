// T3.22: bill writes moved from one await per row to one IN lookup plus a
// batch. sqflite_common_ffi is not a dev dependency, so the resulting "DB
// state" is compared on an in-memory fake of the sqflite executor API: the old
// per-bill loop (copied verbatim below) and the new datasource run against
// identical starting stores and must leave identical rows behind.
import 'dart:math';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/data/models/bill_item_model.dart';
import 'package:uswatte/features/bills/data/models/bill_model.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';

// ── In-memory store + fakes ──────────────────────────────────────────────────

class _Store {
  final Map<String, Map<String, Object?>> bills = {};
  final List<Map<String, Object?>> items = [];
  int queries = 0;
  int maxInParams = 0;
  final List<bool?> commitsNoResult = [];

  _Store copy() {
    final s = _Store();
    bills.forEach((k, v) => s.bills[k] = Map.of(v));
    s.items.addAll(items.map(Map.of));
    return s;
  }

  void insert(String table, Map<String, Object?> values,
      ConflictAlgorithm? conflictAlgorithm) {
    if (table == 'bills') {
      final id = values['client_bill_id'] as String;
      if (bills.containsKey(id) &&
          conflictAlgorithm != ConflictAlgorithm.replace) {
        throw StateError('UNIQUE constraint failed: bills.client_bill_id');
      }
      bills[id] = Map.of(values);
    } else if (table == 'bill_items') {
      items.add(Map.of(values));
    } else {
      throw UnsupportedError(table);
    }
  }

  int delete(String table, String? where, List<Object?>? whereArgs) {
    expect(table, 'bill_items');
    expect(where, 'client_bill_id = ?');
    final before = items.length;
    items.removeWhere((r) => r['client_bill_id'] == whereArgs!.single);
    return before - items.length;
  }

  List<Map<String, Object?>> query(String table, List<String>? columns,
      String? where, List<Object?>? whereArgs, int? limit) {
    expect(table, 'bills');
    queries++;
    final Iterable<String> ids;
    if (where == 'client_bill_id = ?') {
      ids = [whereArgs!.single as String];
    } else {
      expect(where, startsWith('client_bill_id IN ('));
      final placeholders = '?'.allMatches(where!).length;
      expect(placeholders, whereArgs!.length);
      maxInParams = max(maxInParams, whereArgs.length);
      ids = whereArgs.cast<String>();
    }
    final rows = [
      for (final id in ids)
        if (bills.containsKey(id))
          {for (final c in columns!) c: bills[id]![c]},
    ];
    return limit == null ? rows : rows.take(limit).toList();
  }
}

class _FakeBatch extends Fake implements Batch {
  final _Store store;
  final List<void Function()> _ops = [];
  _FakeBatch(this.store);

  @override
  void insert(String table, Map<String, Object?> values,
      {String? nullColumnHack, ConflictAlgorithm? conflictAlgorithm}) {
    final copy = Map.of(values);
    _ops.add(() => store.insert(table, copy, conflictAlgorithm));
  }

  @override
  void delete(String table, {String? where, List<Object?>? whereArgs}) {
    _ops.add(() => store.delete(table, where, whereArgs));
  }

  @override
  Future<List<Object?>> commit(
      {bool? exclusive, bool? noResult, bool? continueOnError}) async {
    store.commitsNoResult.add(noResult);
    for (final op in _ops) {
      op();
    }
    return const [];
  }
}

class _FakeTxn extends Fake implements Transaction {
  final _Store store;
  _FakeTxn(this.store);

  @override
  Future<List<Map<String, Object?>>> query(String table,
          {bool? distinct,
          List<String>? columns,
          String? where,
          List<Object?>? whereArgs,
          String? groupBy,
          String? having,
          String? orderBy,
          int? limit,
          int? offset}) async =>
      store.query(table, columns, where, whereArgs, limit);

  @override
  Future<int> insert(String table, Map<String, Object?> values,
      {String? nullColumnHack, ConflictAlgorithm? conflictAlgorithm}) async {
    store.insert(table, values, conflictAlgorithm);
    return 0;
  }

  @override
  Future<int> delete(String table,
          {String? where, List<Object?>? whereArgs}) async =>
      store.delete(table, where, whereArgs);

  @override
  Batch batch() => _FakeBatch(store);
}

class _FakeDb extends Fake implements Database {
  final _Store store;
  _FakeDb(this.store);

  @override
  Future<T> transaction<T>(Future<T> Function(Transaction txn) action,
          {bool? exclusive}) =>
      action(_FakeTxn(store));
}

class _MockHelper extends Mock implements DatabaseHelper {}

// ── The pre-T3.22 implementations, verbatim ──────────────────────────────────

Future<int> _oldUpsertFromServer(Database db, List<BillModel> bills) async {
  if (bills.isEmpty) return 0;
  var written = 0;

  await db.transaction((txn) async {
    for (final bill in bills) {
      final existing = await txn.query(
        'bills',
        columns: ['sync_status'],
        where: 'client_bill_id = ?',
        whereArgs: [bill.clientBillId],
        limit: 1,
      );

      if (existing.isNotEmpty) {
        final status =
            SyncStatusX.fromDb(existing.first['sync_status'] as String);
        if (status != SyncStatus.synced) continue;
      }

      await txn.insert(
        'bills',
        bill.toMap(),
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
      await txn.delete(
        'bill_items',
        where: 'client_bill_id = ?',
        whereArgs: [bill.clientBillId],
      );
      for (final item in bill.items) {
        await txn.insert('bill_items', item.toMap());
      }
      written++;
    }
  });

  return written;
}

Future<void> _oldInsert(Database db, BillModel bill) async {
  await db.transaction((txn) async {
    await txn.insert(
      'bills',
      bill.toMap(),
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
    for (final item in bill.items) {
      await txn.insert('bill_items', item.toMap());
    }
  });
}

// ── Fixtures ─────────────────────────────────────────────────────────────────

final _t = DateTime.utc(2026, 9, 17, 10);

BillModel _bill(
  String id, {
  SyncStatus status = SyncStatus.synced,
  int items = 2,
  double total = 100,
  int? serverId,
  DateTime? lastAttemptAt,
}) =>
    BillModel(
      clientBillId: id,
      outletId: 7,
      billingDate: _t,
      billDiscountRate: 0,
      subTotalAmount: total,
      billDiscountAmount: 0,
      totalAmount: total,
      createdAt: _t,
      syncStatus: status,
      syncAttempts: 3,
      lastSyncError: 'e',
      lastSyncErrorCode: 'E',
      lastAttemptAt: lastAttemptAt,
      serverBillId: serverId,
      serverBillNumber: serverId == null ? null : 'B-$serverId',
      outletName: 'Outlet',
      outletCategory: 'A',
      items: [
        for (var i = 1; i <= items; i++)
          BillItemModel(
            clientBillId: id,
            productId: i,
            quantity: total + i,
            unitPrice: 10,
            lineNumber: i,
          ),
      ],
    );

_Store _seed(Map<String, SyncStatus> existing) {
  final s = _Store();
  existing.forEach((id, status) {
    final b = _bill(id, status: status, items: 3, total: 1);
    s.bills[id] = b.toMap();
    s.items.addAll(b.items.map((i) => i.toMap()));
  });
  return s;
}

void main() {
  late _MockHelper helper;

  Future<(int, _Store, int, _Store)> runBoth(
      _Store start, List<BillModel> bills) async {
    final oldStore = start.copy();
    final newStore = start.copy();
    final oldWritten = await _oldUpsertFromServer(_FakeDb(oldStore), bills);
    when(() => helper.database).thenAnswer((_) async => _FakeDb(newStore));
    final newWritten =
        await BillsLocalDatasource(helper).upsertFromServer(bills);
    return (oldWritten, oldStore, newWritten, newStore);
  }

  void expectSameState(_Store a, _Store b) {
    expect(b.bills, a.bills);
    expect(b.items, a.items);
  }

  setUp(() => helper = _MockHelper());

  test('never overwrites pending/failed/syncing (or cancelled) rows; refreshes '
      'synced rows with item replacement; inserts new keys', () async {
    final start = _seed({
      'p': SyncStatus.pending,
      'f': SyncStatus.failed,
      's': SyncStatus.syncing,
      'c': SyncStatus.cancelled,
      'ok': SyncStatus.synced,
    });
    final server = [
      for (final id in ['p', 'f', 's', 'c', 'ok', 'new'])
        _bill(id, items: 2, total: 500, serverId: 9, lastAttemptAt: _t),
    ];

    final (oldW, oldS, newW, newS) = await runBoth(start, server);

    expect(newW, oldW);
    expect(newW, 2);
    expectSameState(oldS, newS);
    // Local unsynced rows and their lines untouched.
    for (final id in ['p', 'f', 's', 'c']) {
      expect(newS.bills[id]!['total_amount'], 1);
      expect(newS.items.where((r) => r['client_bill_id'] == id), hasLength(3));
    }
    // Synced row replaced, lines replaced (not merged), every column written.
    expect(newS.bills['ok'], server[4].toMap());
    expect(newS.bills['ok']!['last_attempt_at'], _t.toIso8601String());
    expect(newS.items.where((r) => r['client_bill_id'] == 'ok'), hasLength(2));
    expect(newS.bills['new'], server[5].toMap());
    // One IN lookup, one batch without results.
    expect(newS.queries, 1);
    expect(newS.commitsNoResult, [true]);
  });

  test('a key repeated in one page behaves as the sequential loop did',
      () async {
    final start = _seed({'x': SyncStatus.synced});
    final server = [
      _bill('x', total: 10),
      _bill('x', status: SyncStatus.pending, total: 20), // written, then...
      _bill('x', total: 30), // ...skipped: local row is now pending
      _bill('y', items: 1, total: 40),
      _bill('y', items: 3, total: 50),
    ];

    final (oldW, oldS, newW, newS) = await runBoth(start, server);

    expect(newW, oldW);
    expectSameState(oldS, newS);
    expect(newS.bills['x']!['total_amount'], 20);
  });

  test('IN lookups are chunked to at most 500 params and match the old loop',
      () async {
    final existing = <String, SyncStatus>{
      for (var i = 0; i < 1200; i += 3)
        'b$i': SyncStatus.values[i % SyncStatus.values.length],
    };
    final start = _seed(existing);
    final server = [for (var i = 0; i < 1234; i++) _bill('b$i', items: i % 4)];

    final (oldW, oldS, newW, newS) = await runBoth(start, server);

    expect(newW, oldW);
    expectSameState(oldS, newS);
    expect(newS.queries, 3); // 500 + 500 + 234
    expect(newS.maxInParams, 500);
  });

  test('randomised pages leave the same rows as the old loop', () async {
    final rnd = Random(42);
    for (var round = 0; round < 50; round++) {
      final existing = <String, SyncStatus>{
        for (var i = 0; i < 30; i++)
          if (rnd.nextBool())
            'k$i': SyncStatus.values[rnd.nextInt(SyncStatus.values.length)],
      };
      final server = [
        for (var i = 0; i < rnd.nextInt(40); i++)
          _bill(
            'k${rnd.nextInt(30)}',
            status: SyncStatus.values[rnd.nextInt(SyncStatus.values.length)],
            items: rnd.nextInt(4),
            total: rnd.nextInt(1000).toDouble(),
          ),
      ];

      final (oldW, oldS, newW, newS) = await runBoth(_seed(existing), server);

      expect(newW, oldW, reason: 'round $round');
      expectSameState(oldS, newS);
    }
  });

  test('empty page writes nothing and opens no transaction', () async {
    expect(await BillsLocalDatasource(helper).upsertFromServer(const []), 0);
    verifyNever(() => helper.database);
  });

  test('insert writes the bill and its lines exactly as before', () async {
    for (final bill in [_bill('n', items: 0), _bill('m', items: 5)]) {
      final oldStore = _seed({'m': SyncStatus.pending});
      final newStore = _seed({'m': SyncStatus.pending});
      await _oldInsert(_FakeDb(oldStore), bill);
      when(() => helper.database).thenAnswer((_) async => _FakeDb(newStore));
      await BillsLocalDatasource(helper).insert(bill);
      expectSameState(oldStore, newStore);
    }
  });

  test('chunkForInClause keeps order and bounds', () {
    expect(chunkForInClause(const []), isEmpty);
    final ids = [for (var i = 0; i < 1001; i++) '$i'];
    final chunks = chunkForInClause(ids);
    expect(chunks.map((c) => c.length), [500, 500, 1]);
    expect(chunks.expand((c) => c), ids);
  });
}
