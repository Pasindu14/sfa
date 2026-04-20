import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/outlets/data/models/outlet_model.dart';

class OutletsLocalDatasource {
  final DatabaseHelper _dbHelper;

  const OutletsLocalDatasource(this._dbHelper);

  Future<List<OutletModel>> getAllOutlets() async {
    final db = await _dbHelper.database;
    final rows = await db.query('daily_outlets', orderBy: 'name ASC');
    return rows.map(OutletModel.fromMap).toList();
  }

  Future<void> replaceAll(List<OutletModel> outlets) async {
    final db = await _dbHelper.database;
    await db.transaction((txn) async {
      await txn.delete('daily_outlets');
      if (outlets.isEmpty) return;
      final batch = txn.batch();
      for (final o in outlets) {
        batch.insert('daily_outlets', o.toMap());
      }
      await batch.commit(noResult: true);
    });
  }

  Future<DateTime?> getLastSyncedAt() async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'metadata',
      where: 'key = ?',
      whereArgs: ['daily_outlets_last_synced_at'],
    );
    if (rows.isEmpty) return null;
    return DateTime.tryParse(rows.first['value'] as String);
  }

  Future<void> saveLastSyncedAt(DateTime dt) async {
    final db = await _dbHelper.database;
    await db.insert(
      'metadata',
      {'key': 'daily_outlets_last_synced_at', 'value': dt.toIso8601String()},
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  Future<void> saveCurrentRoute(int routeId, String routeName) async {
    final db = await _dbHelper.database;
    await db.insert(
      'metadata',
      {'key': 'current_route_id', 'value': routeId.toString()},
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
    await db.insert(
      'metadata',
      {'key': 'current_route_name', 'value': routeName},
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  Future<int?> getCurrentRouteId() async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'metadata',
      where: 'key = ?',
      whereArgs: ['current_route_id'],
    );
    if (rows.isEmpty) return null;
    return int.tryParse(rows.first['value'] as String);
  }
}
