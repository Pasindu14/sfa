import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/products/data/models/product_category_model.dart';

class ProductCategoriesLocalDatasource {
  final DatabaseHelper _dbHelper;

  const ProductCategoriesLocalDatasource(this._dbHelper);

  Future<List<ProductCategoryModel>> getAll() async {
    final db = await _dbHelper.database;
    final rows = await db.query('product_categories', orderBy: 'sort_order ASC, id ASC');
    return rows.map(ProductCategoryModel.fromMap).toList();
  }

  /// Atomically clears the product_categories table and inserts [categories].
  Future<void> replaceAll(List<ProductCategoryModel> categories) async {
    final db = await _dbHelper.database;
    await db.transaction((txn) async {
      await txn.delete('product_categories');
      if (categories.isEmpty) return;
      final batch = txn.batch();
      for (final c in categories) {
        batch.insert('product_categories', c.toMap());
      }
      await batch.commit(noResult: true);
    });
  }

  Future<DateTime?> getLastSyncedAt() async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'metadata',
      where: 'key = ?',
      whereArgs: ['product_categories_last_synced_at'],
    );
    if (rows.isEmpty) return null;
    return DateTime.tryParse(rows.first['value'] as String);
  }

  Future<void> saveLastSyncedAt(DateTime dt) async {
    final db = await _dbHelper.database;
    await db.insert(
      'metadata',
      {'key': 'product_categories_last_synced_at', 'value': dt.toIso8601String()},
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }
}
