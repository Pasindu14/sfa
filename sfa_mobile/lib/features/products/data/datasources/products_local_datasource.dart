import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/products/data/models/product_model.dart';

class ProductsLocalDatasource {
  final DatabaseHelper _dbHelper;

  const ProductsLocalDatasource(this._dbHelper);

  Future<List<ProductModel>> getAllProducts() async {
    final db = await _dbHelper.database;
    final rows = await db.query('products', orderBy: 'code ASC');
    return rows.map(ProductModel.fromMap).toList();
  }

  /// Atomically clears the products table and inserts [products].
  /// Uses a transaction + batch so it's safe under any list size.
  Future<void> replaceAll(List<ProductModel> products) async {
    final db = await _dbHelper.database;
    await db.transaction((txn) async {
      await txn.delete('products');
      if (products.isEmpty) return;
      final batch = txn.batch();
      for (final p in products) {
        batch.insert('products', p.toMap());
      }
      await batch.commit(noResult: true);
    });
  }

  /// True when the local products table holds at least one row. A conditional
  /// GET is only safe against a non-empty cache — a 304 would otherwise leave
  /// the rep with no products.
  Future<bool> hasAny() async {
    final db = await _dbHelper.database;
    final rows = await db.rawQuery('SELECT 1 FROM products LIMIT 1');
    return rows.isNotEmpty;
  }

  Future<DateTime?> getLastSyncedAt() async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'metadata',
      where: 'key = ?',
      whereArgs: ['products_last_synced_at'],
    );
    if (rows.isEmpty) return null;
    return DateTime.tryParse(rows.first['value'] as String);
  }

  Future<void> saveLastSyncedAt(DateTime dt) async {
    final db = await _dbHelper.database;
    await db.insert(
      'metadata',
      {'key': 'products_last_synced_at', 'value': dt.toIso8601String()},
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }
}
