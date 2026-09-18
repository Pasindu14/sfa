// Guards the v21 read-path indexes.
//
// The test suite has no real SQLite (no sqflite_common_ffi), so the actual
// migration can't be executed here. What is checked instead is the contract
// that makes it safe for the multi-isolate ROLLBACK race described on
// DatabaseHelper: every statement is re-runnable, every indexed column is
// part of the table definition, and the statements are wired into the
// onCreate, the v21 upgrade step and the always-run repair pass.
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';

class _RecordingDb extends Fake implements DatabaseExecutor {
  final executed = <String>[];

  @override
  Future<void> execute(String sql, [List<Object?>? arguments]) async {
    executed.add(sql);
  }
}

String _helperSource() => File(
  'lib/core/db/database_helper.dart',
).readAsStringSync().replaceAll('\r\n', '\n');

/// Column names inside `CREATE TABLE IF NOT EXISTS <table> ( ... )`.
Set<String> _columnsOf(String source, String table) {
  final m = RegExp(
    'CREATE TABLE IF NOT EXISTS $table \\((.*?)\\n\\s*\\)',
    dotAll: true,
  ).firstMatch(source);
  expect(m, isNotNull, reason: 'no CREATE TABLE for $table');
  return m!
      .group(1)!
      .split('\n')
      .map((l) => l.trim())
      .where((l) => l.isNotEmpty)
      .map((l) => l.split(RegExp(r'\s+')).first)
      .toSet();
}

void main() {
  const expected = {
    'bills': ['created_at', 'billing_date'],
    'products': ['code', 'category_id'],
  };

  test('creates exactly the four read-path indexes', () async {
    final db = _RecordingDb();
    await DatabaseHelper.ensurePerformanceIndexes(db);

    final targets = db.executed.map((sql) {
      final m = RegExp(r'ON (\w+)\((\w+)\)').firstMatch(sql)!;
      return '${m.group(1)}.${m.group(2)}';
    }).toSet();
    expect(targets, {
      for (final e in expected.entries)
        for (final c in e.value) '${e.key}.$c',
    });
  });

  test('every statement is idempotent and running twice is harmless', () async {
    for (final sql in DatabaseHelper.performanceIndexStatements) {
      expect(sql, startsWith('CREATE INDEX IF NOT EXISTS '));
    }
    final db = _RecordingDb();
    await DatabaseHelper.ensurePerformanceIndexes(db);
    await DatabaseHelper.ensurePerformanceIndexes(db);
    expect(db.executed, [
      ...DatabaseHelper.performanceIndexStatements,
      ...DatabaseHelper.performanceIndexStatements,
    ]);
  });

  test('index names do not collide with existing indexes', () {
    final source = _helperSource();
    for (final sql in DatabaseHelper.performanceIndexStatements) {
      final name = RegExp(r'EXISTS (\w+) ON').firstMatch(sql)!.group(1)!;
      // Once in the list above, nowhere else.
      expect(RegExp('\\b$name\\b').allMatches(source).length, 1, reason: name);
    }
  });

  test('every indexed column exists in its CREATE TABLE', () {
    final source = _helperSource();
    for (final e in expected.entries) {
      expect(_columnsOf(source, e.key), containsAll(e.value), reason: e.key);
    }
  });

  test('wired into version 21, the upgrade ladder and the repair pass', () {
    final source = _helperSource();
    // At least 21: later versions keep the v21 step on the ladder.
    final version = int.parse(
      RegExp(r'static const _dbVersion = (\d+);').firstMatch(source)!.group(1)!,
    );
    expect(version, greaterThanOrEqualTo(21));
    expect(
      source,
      contains('if (oldVersion < 21) await _createPerformanceIndexesV21(db);'),
    );

    // _ensureSchemaColumns runs at the end of both onCreate and onUpgrade; the
    // indexes must be its last step so every column already exists.
    final repair = RegExp(
      r'Future<void> _ensureSchemaColumns\(Database db\) async \{(.*?)\n  \}',
      dotAll: true,
    ).firstMatch(source)!.group(1)!.trim();
    expect(repair, endsWith('await ensurePerformanceIndexes(db);'));
  });
}
