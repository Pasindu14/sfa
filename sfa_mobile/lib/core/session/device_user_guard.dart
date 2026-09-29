import 'package:flutter/foundation.dart';
import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';

/// Keeps one user's on-device data from leaking into the next user's session
/// on a shared phone.
///
/// The local tables below carry no owner column, so every screen reads them as
/// "mine" — and the outbox rows are uploaded under whoever's JWT is current,
/// which would attribute rep A's bills to rep B. Instead of wiping on logout
/// (which would throw away unsynced work when the same rep signs straight back
/// in), the device remembers who last signed in and only clears when a
/// *different* user does.
class DeviceUserGuard {
  static const lastUserIdKey = 'last_user_id';

  /// Per-user metadata rows (sync stamps, today's route, geofence policy,
  /// stock stamp). Master data stamps (products, pricing) are shared and kept.
  static const _userMetadataKeys = [
    'daily_outlets_last_synced_at',
    'current_route_id',
    'current_route_name',
    'geofence_radius_meters',
    'geofence_enforced',
    'geofence_enforced_from',
    'geofence_exemption_reason',
    'distributor_stocks_last_synced_at',
  ];

  static const _unsynced = "sync_status IN ('pending', 'failed')";

  final DatabaseHelper _dbHelper;

  const DeviceUserGuard(this._dbHelper);

  /// Call after a successful interactive login, before the session is exposed
  /// to the UI or the post-login sync.
  ///
  /// - same user as last time → nothing is touched (unsynced work survives)
  /// - different user → all per-user data is cleared, outbox included
  /// - no record (first login on this install / app upgraded from a build
  ///   without this guard) → synced/cached per-user data is cleared, but the
  ///   outbox is kept since it most likely belongs to this same rep
  Future<void> onLogin(String userId) async {
    final db = await _dbHelper.database;
    final previous = await _readLastUserId(db);
    if (previous == userId) return;

    await db.transaction((txn) async {
      if (previous == null) {
        await _clearSyncedOnly(txn);
      } else {
        await _logDiscardedOutbox(txn, previous, userId);
        await _clearAll(txn);
      }
      await txn.insert(
        'metadata',
        {'key': lastUserIdKey, 'value': userId},
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    });
  }

  /// Session restored from storage on app start — the same user never left,
  /// so nothing is cleared; just backfill the owner on installs upgraded from
  /// a build that didn't record it.
  Future<void> recordIfUnknown(String userId) async {
    final db = await _dbHelper.database;
    if (await _readLastUserId(db) != null) return;
    await db.insert(
      'metadata',
      {'key': lastUserIdKey, 'value': userId},
      conflictAlgorithm: ConflictAlgorithm.ignore,
    );
  }

  Future<String?> _readLastUserId(DatabaseExecutor db) async {
    final rows = await db.query(
      'metadata',
      columns: ['value'],
      where: 'key = ?',
      whereArgs: [lastUserIdKey],
    );
    if (rows.isEmpty) return null;
    final value = rows.first['value'] as String?;
    return (value == null || value.isEmpty) ? null : value;
  }

  Future<void> _clearAll(Transaction txn) async {
    // bill_items first: foreign_keys is not enabled, so ON DELETE CASCADE
    // does not fire.
    await txn.delete('bill_items');
    await txn.delete('bills');
    await txn.delete('not_billings');
    await txn.delete('pending_location_pings');
    await _clearCaches(txn);
  }

  Future<void> _clearSyncedOnly(Transaction txn) async {
    await txn.rawDelete(
      'DELETE FROM bill_items WHERE client_bill_id IN '
      '(SELECT client_bill_id FROM bills WHERE NOT ($_unsynced))',
    );
    await txn.delete('bills', where: 'NOT ($_unsynced)');
    await txn.delete('not_billings', where: 'NOT ($_unsynced)');
    await _clearCaches(txn);
  }

  Future<void> _clearCaches(Transaction txn) async {
    await txn.delete('daily_outlets');
    await txn.delete('distributor_stocks');
    await txn.delete(
      'metadata',
      where: 'key IN (${List.filled(_userMetadataKeys.length, '?').join(', ')})',
      whereArgs: _userMetadataKeys,
    );
  }

  /// Unsynced rows of the previous user are lost at this point — the logout
  /// prompt tries to flush them first, so this should be rare. Leave a trace.
  Future<void> _logDiscardedOutbox(
      Transaction txn, String previous, String current) async {
    int count(List<Map<String, Object?>> r) => (r.first['c'] as int?) ?? 0;
    final bills = count(await txn
        .rawQuery('SELECT COUNT(*) AS c FROM bills WHERE $_unsynced'));
    final notBillings = count(await txn
        .rawQuery('SELECT COUNT(*) AS c FROM not_billings WHERE $_unsynced'));
    if (bills > 0 || notBillings > 0) {
      debugPrint('DeviceUserGuard: user $previous -> $current, discarding '
          '$bills unsynced bill(s) and $notBillings unsynced not-billing(s)');
    }
  }
}
