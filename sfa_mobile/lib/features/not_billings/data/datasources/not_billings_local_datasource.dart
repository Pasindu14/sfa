import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/not_billings/data/models/not_billing_model.dart';

class NotBillingsLocalDatasource {
  final DatabaseHelper _dbHelper;

  const NotBillingsLocalDatasource(this._dbHelper);

  Future<void> insert(NotBillingModel record) async {
    final db = await _dbHelper.database;
    await db.insert(
      'not_billings',
      record.toMap(),
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  Future<List<NotBillingModel>> getAll({int? limit}) async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'not_billings',
      orderBy: 'created_at DESC',
      limit: limit,
    );
    return rows.map(NotBillingModel.fromMap).toList();
  }

  Future<NotBillingModel?> getById(String clientNotBillingId) async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'not_billings',
      where: 'client_not_billing_id = ?',
      whereArgs: [clientNotBillingId],
      limit: 1,
    );
    if (rows.isEmpty) return null;
    return NotBillingModel.fromMap(rows.first);
  }

  Future<List<NotBillingModel>> getPendingForSync() async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'not_billings',
      where: "sync_status IN ('pending', 'failed')",
      orderBy: 'created_at ASC',
    );
    return rows.map(NotBillingModel.fromMap).toList();
  }

  Future<int> countPendingOrFailed() async {
    final db = await _dbHelper.database;
    final rows = await db.rawQuery(
      "SELECT COUNT(*) AS c FROM not_billings WHERE sync_status IN ('pending', 'failed')",
    );
    return (rows.first['c'] as int?) ?? 0;
  }

  Future<void> markSyncing(String clientNotBillingId) async {
    final db = await _dbHelper.database;
    await db.update(
      'not_billings',
      {'sync_status': SyncStatus.syncing.dbValue},
      where: 'client_not_billing_id = ?',
      whereArgs: [clientNotBillingId],
    );
  }

  Future<void> markSynced(
    String clientNotBillingId, {
    required int serverNotBillingId,
    required String serverNotBillingNumber,
  }) async {
    final db = await _dbHelper.database;
    await db.update(
      'not_billings',
      {
        'sync_status': SyncStatus.synced.dbValue,
        'server_not_billing_id': serverNotBillingId,
        'server_not_billing_number': serverNotBillingNumber,
        'last_sync_error': null,
        'last_sync_error_code': null,
      },
      where: 'client_not_billing_id = ?',
      whereArgs: [clientNotBillingId],
    );
  }

  Future<void> markFailed(
    String clientNotBillingId, {
    required String errorCode,
    required String errorMessage,
  }) async {
    final db = await _dbHelper.database;
    await db.rawUpdate(
      '''UPDATE not_billings
         SET sync_status = ?,
             sync_attempts = sync_attempts + 1,
             last_sync_error_code = ?,
             last_sync_error = ?
         WHERE client_not_billing_id = ?''',
      [SyncStatus.failed.dbValue, errorCode, errorMessage, clientNotBillingId],
    );
  }

  Future<void> markPendingAfterNetworkError(String clientNotBillingId) async {
    final db = await _dbHelper.database;
    await db.rawUpdate(
      '''UPDATE not_billings
         SET sync_status = ?,
             sync_attempts = sync_attempts + 1
         WHERE client_not_billing_id = ?''',
      [SyncStatus.pending.dbValue, clientNotBillingId],
    );
  }

  Future<void> delete(String clientNotBillingId) async {
    final db = await _dbHelper.database;
    await db.delete(
      'not_billings',
      where: 'client_not_billing_id = ? AND sync_status != ?',
      whereArgs: [clientNotBillingId, SyncStatus.synced.dbValue],
    );
  }

  Future<Set<int>> getTodaysNotBilledOutletIds() async {
    final db = await _dbHelper.database;
    final rows = await db.rawQuery(
      "SELECT DISTINCT outlet_id FROM not_billings WHERE not_billing_date = DATE('now')",
    );
    return rows.map((r) => r['outlet_id'] as int).toSet();
  }

  Future<int> purgeSyncedOlderThan(DateTime cutoff) async {
    final db = await _dbHelper.database;
    final cutoffIso = cutoff.toUtc().toIso8601String();
    return db.delete(
      'not_billings',
      where: 'sync_status = ? AND created_at < ?',
      whereArgs: [SyncStatus.synced.dbValue, cutoffIso],
    );
  }
}
