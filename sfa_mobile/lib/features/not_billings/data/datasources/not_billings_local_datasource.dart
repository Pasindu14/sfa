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

  /// Atomically claims a row for sending: pending/failed -> syncing, stamping
  /// the claim time in `last_attempt_at`. Returns true only when THIS call made
  /// the change, so two senders racing for one row cannot both POST it. See
  /// BillsLocalDatasource.claimForSync.
  Future<bool> claimForSync(String clientNotBillingId, {DateTime? now}) async {
    final db = await _dbHelper.database;
    final changed = await db.rawUpdate(
      '''UPDATE not_billings
         SET sync_status = ?,
             last_attempt_at = ?
         WHERE client_not_billing_id = ?
           AND sync_status IN ('pending', 'failed')''',
      [
        SyncStatus.syncing.dbValue,
        (now ?? DateTime.now()).toUtc().toIso8601String(),
        clientNotBillingId,
      ],
    );
    return changed == 1;
  }

  /// Puts rows stuck in `syncing` back to `pending` so they are counted and
  /// retried again. Rows in [inFlightIds] (sent by THIS isolate) and rows
  /// claimed after [staleBefore] (possibly sent by another isolate) are left
  /// alone. Re-sending is safe: the stable client id is the idempotency key.
  /// Returns how many rows were reset.
  Future<int> resetStaleSyncing({
    required DateTime staleBefore,
    Set<String> inFlightIds = const {},
  }) async {
    final db = await _dbHelper.database;
    final skip = inFlightIds.isEmpty
        ? ''
        : 'AND client_not_billing_id NOT IN '
            '(${List.filled(inFlightIds.length, '?').join(',')})';
    return db.rawUpdate(
      '''UPDATE not_billings
         SET sync_status = ?
         WHERE sync_status = ?
           AND (last_attempt_at IS NULL OR last_attempt_at < ?)
           $skip''',
      [
        SyncStatus.pending.dbValue,
        SyncStatus.syncing.dbValue,
        staleBefore.toUtc().toIso8601String(),
        ...inFlightIds,
      ],
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
             last_sync_error = ?,
             last_attempt_at = ?
         WHERE client_not_billing_id = ?
           AND sync_status NOT IN ('synced', 'cancelled')''',
      [
        SyncStatus.failed.dbValue,
        errorCode,
        errorMessage,
        DateTime.now().toUtc().toIso8601String(),
        clientNotBillingId,
      ],
    );
  }

  /// Never touches a row that is already synced or cancelled — a late failure
  /// report must not downgrade it (see [markFailed]).
  Future<void> markPendingAfterNetworkError(String clientNotBillingId) async {
    final db = await _dbHelper.database;
    await db.rawUpdate(
      '''UPDATE not_billings
         SET sync_status = ?,
             sync_attempts = sync_attempts + 1,
             last_attempt_at = ?
         WHERE client_not_billing_id = ?
           AND sync_status NOT IN ('synced', 'cancelled')''',
      [
        SyncStatus.pending.dbValue,
        DateTime.now().toUtc().toIso8601String(),
        clientNotBillingId,
      ],
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
