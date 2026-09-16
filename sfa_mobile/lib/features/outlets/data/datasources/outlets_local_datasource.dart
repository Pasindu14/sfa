import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/outlets/data/models/outlet_model.dart';
import 'package:uswatte/features/outlets/domain/entities/proximity_policy.dart';

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

  /// Wipes today's outlet snapshot and its sync stamp. Called when a sync
  /// confirms there is no route assignment for today, so a stale "today"
  /// timestamp from an earlier (possibly stale-route) sync can't keep
  /// OutletsBloc's `_isSyncedToday` check believing outlets are still valid.
  Future<void> clearDailyOutlets() async {
    final db = await _dbHelper.database;
    await db.transaction((txn) async {
      await txn.delete('daily_outlets');
      await txn.delete(
        'metadata',
        where: 'key = ?',
        whereArgs: ['daily_outlets_last_synced_at'],
      );
    });
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

  Future<void> stampLastBillDate(int outletId, DateTime date) async {
    final db = await _dbHelper.database;
    await db.update(
      'daily_outlets',
      {'last_bill_date': date.toIso8601String()},
      where: 'id = ?',
      whereArgs: [outletId],
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

  Future<String?> getCurrentRouteName() async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'metadata',
      where: 'key = ?',
      whereArgs: ['current_route_name'],
    );
    if (rows.isEmpty) return null;
    return rows.first['value'] as String?;
  }

  /// Caches the whole geofence policy — radius plus whether it is currently
  /// enforced and, if not, when enforcement resumes.
  ///
  /// Stored as separate `metadata` keys rather than a new table: the existing
  /// `geofence_radius_meters` key already lives here, and adding keys needs no
  /// schema version bump (which would have to be idempotent across the three
  /// isolates sharing this database).
  Future<void> saveProximityPolicy(ProximityPolicy policy) async {
    final db = await _dbHelper.database;
    final batch = db.batch();
    void put(String key, String value) => batch.insert(
          'metadata',
          {'key': key, 'value': value},
          conflictAlgorithm: ConflictAlgorithm.replace,
        );

    put('geofence_radius_meters', policy.radiusMeters.toString());
    put('geofence_enforced', policy.enforced ? '1' : '0');
    // Null means "nothing scheduled" — write the empty string rather than
    // deleting, so a stale value from a previous grant can never be read back.
    put('geofence_enforced_from', policy.enforcedFrom?.toUtc().toIso8601String() ?? '');
    put('geofence_exemption_reason', policy.exemptionReason ?? '');

    await batch.commit(noResult: true);
  }

  Future<ProximityPolicy?> getProximityPolicy() async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'metadata',
      where: 'key IN (?, ?, ?, ?)',
      whereArgs: const [
        'geofence_radius_meters',
        'geofence_enforced',
        'geofence_enforced_from',
        'geofence_exemption_reason',
      ],
    );
    if (rows.isEmpty) return null;

    final map = {
      for (final r in rows) r['key'] as String: r['value'] as String,
    };

    final radius = double.tryParse(map['geofence_radius_meters'] ?? '');
    if (radius == null) return null;

    final from = map['geofence_enforced_from'];
    final reason = map['geofence_exemption_reason'];

    return ProximityPolicy(
      // Absent key = a device that cached a radius before exemptions existed.
      // Default to enforced.
      enforced: (map['geofence_enforced'] ?? '1') == '1',
      radiusMeters: radius,
      enforcedFrom:
          (from == null || from.isEmpty) ? null : DateTime.tryParse(from),
      exemptionReason: (reason == null || reason.isEmpty) ? null : reason,
    );
  }
}
