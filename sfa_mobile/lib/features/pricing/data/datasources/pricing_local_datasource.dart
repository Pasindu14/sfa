import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/pricing/data/models/pricing_item_model.dart';
import 'package:uswatte/features/pricing/data/models/pricing_structure_model.dart';

class PricingLocalDatasource {
  final DatabaseHelper _dbHelper;

  const PricingLocalDatasource(this._dbHelper);

  Future<List<PricingStructureModel>> getAllStructures() async {
    final db = await _dbHelper.database;
    final structureRows =
        await db.query('pricing_structures', orderBy: 'is_default DESC, name ASC');
    final itemRows = await db.query('pricing_items', orderBy: 'product_code ASC');

    final itemsByStructure = <int, List<PricingItemModel>>{};
    for (final row in itemRows) {
      final structureId = row['pricing_structure_id'] as int;
      itemsByStructure
          .putIfAbsent(structureId, () => [])
          .add(PricingItemModel.fromMap(row));
    }

    return structureRows.map((row) {
      final id = row['id'] as int;
      return PricingStructureModel.fromMap(row, itemsByStructure[id] ?? []);
    }).toList();
  }

  Future<void> replaceAll(List<PricingStructureModel> structures) async {
    final db = await _dbHelper.database;
    await db.transaction((txn) async {
      await txn.delete('pricing_items');
      await txn.delete('pricing_structures');
      if (structures.isEmpty) return;

      final batch = txn.batch();
      for (final s in structures) {
        batch.insert('pricing_structures', s.toMap());
        for (final item in s.items) {
          batch.insert('pricing_items', item.toMap());
        }
      }
      await batch.commit(noResult: true);
    });
  }

  Future<DateTime?> getLastSyncedAt() async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'metadata',
      where: 'key = ?',
      whereArgs: ['pricing_last_synced_at'],
    );
    if (rows.isEmpty) return null;
    return DateTime.tryParse(rows.first['value'] as String);
  }

  Future<void> saveLastSyncedAt(DateTime dt) async {
    final db = await _dbHelper.database;
    await db.insert(
      'metadata',
      {'key': 'pricing_last_synced_at', 'value': dt.toIso8601String()},
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }
}
