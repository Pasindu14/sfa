// Guards the v22 Pricing Structures schema.
//
// No real SQLite in the test suite (no sqflite_common_ffi), so — like
// performance_indexes_test.dart — this checks the source contract instead:
// the new tables use names the v16 teardown does not drop, every statement is
// re-runnable (see the cross-isolate ROLLBACK note on DatabaseHelper), and the
// new columns are both in the CREATE TABLEs and in the always-run repair pass.
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

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

String _body(String source, String signature) => RegExp(
  '${RegExp.escape(signature)} async \\{(.*?)\\n  \\}',
  dotAll: true,
).firstMatch(source)!.group(1)!;

void main() {
  test('version 22 is on the upgrade ladder', () {
    final source = _helperSource();
    expect(source, contains('static const _dbVersion = 22;'));
    expect(
      source,
      contains('if (oldVersion < 22) await _migratePricingStructuresV22(db);'),
    );
  });

  test('new tables avoid the names the v16 step drops', () {
    final source = _helperSource();
    expect(_columnsOf(source, 'price_structures'),
        containsAll(['id', 'name', 'is_default']));
    expect(
      _columnsOf(source, 'price_structure_items'),
      containsAll([
        'structure_id',
        'product_id',
        'dealer_pack_price',
        'dealer_case_price',
        'mrp',
      ]),
    );
    // The v16 teardown still targets the old names only.
    expect(source, contains('DROP TABLE IF EXISTS pricing_items'));
    expect(source, contains('DROP TABLE IF EXISTS pricing_structures'));
    expect(source, isNot(contains('DROP TABLE IF EXISTS price_structure')));
  });

  test('tables are created on every open, before the ladder', () {
    final source = _helperSource();
    expect(
      _body(source, 'Future<void> _createAllTables(Database db)'),
      contains('await _createPriceStructuresTables(db);'),
    );
    final create =
        _body(source, 'Future<void> _createPriceStructuresTables(Database db)');
    expect(
      RegExp('CREATE TABLE ').allMatches(create).length,
      RegExp('CREATE TABLE IF NOT EXISTS ').allMatches(create).length,
    );
  });

  test('new bill columns are in the CREATE TABLEs and the repair pass', () {
    final source = _helperSource();
    expect(_columnsOf(source, 'bills'), contains('pricing_structure_id'));
    expect(
      _columnsOf(source, 'bill_items'),
      containsAll(['pricing_structure_id', 'list_unit_price']),
    );

    final repair =
        _body(source, 'Future<void> _ensureSchemaColumns(Database db)');
    for (final call in [
      "_ensureColumn(db, 'bills', 'pricing_structure_id', 'INTEGER')",
      "_ensureColumn(db, 'bill_items', 'pricing_structure_id', 'INTEGER')",
      "_ensureColumn(db, 'bill_items', 'list_unit_price', 'REAL')",
    ]) {
      expect(repair, contains(call));
    }
    // Indexes stay the last step of the repair pass.
    expect(repair.trim(), endsWith('await ensurePerformanceIndexes(db);'));
  });
}
