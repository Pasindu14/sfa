import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';

/// Stored HTTP ETags for master-data downloads that support conditional GET.
///
/// Kept as rows in the existing `metadata` key-value table, so there is no
/// schema change and a DB wipe/reinstall drops them together with the cached
/// rows they describe.
class EtagStore {
  static const String products = 'products_etag';
  static const String productCategories = 'product_categories_etag';
  static const String pricingStructures = 'pricing_structures_etag';

  static const List<String> allKeys = [
    products,
    productCategories,
    pricingStructures,
  ];

  final DatabaseHelper _dbHelper;

  const EtagStore(this._dbHelper);

  Future<String?> read(String key) async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'metadata',
      columns: ['value'],
      where: 'key = ?',
      whereArgs: [key],
    );
    if (rows.isEmpty) return null;
    final value = rows.first['value'] as String?;
    return (value == null || value.isEmpty) ? null : value;
  }

  Future<void> write(String key, String etag) async {
    final db = await _dbHelper.database;
    await db.insert(
      'metadata',
      {'key': key, 'value': etag},
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  Future<void> clear(String key) async {
    final db = await _dbHelper.database;
    await db.delete('metadata', where: 'key = ?', whereArgs: [key]);
  }

  /// Called on logout / session expiry so the next user always gets a full
  /// download instead of trusting the previous user's cache.
  Future<void> clearAll() async {
    final db = await _dbHelper.database;
    await db.delete(
      'metadata',
      where: 'key IN (${List.filled(allKeys.length, '?').join(', ')})',
      whereArgs: allKeys,
    );
  }
}
