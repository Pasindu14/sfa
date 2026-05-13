import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/stock/data/models/distributor_stock_model.dart';

class StockWithProduct {
  final int productId;
  final String productCode;
  final String productName;
  final String stockType;
  final double quantityOnHand;
  final String lastUpdatedAt;

  const StockWithProduct({
    required this.productId,
    required this.productCode,
    required this.productName,
    required this.stockType,
    required this.quantityOnHand,
    required this.lastUpdatedAt,
  });

  static StockWithProduct _fromRow(Map<String, dynamic> r) => StockWithProduct(
        productId: r['product_id'] as int,
        productCode: (r['product_code'] as String?) ?? '—',
        productName: (r['product_name'] as String?) ?? 'Unknown Product',
        stockType: r['stock_type'] as String,
        quantityOnHand: (r['quantity_on_hand'] as num).toDouble(),
        lastUpdatedAt: r['last_updated_at'] as String,
      );
}

class DistributorStockLocalDatasource {
  final DatabaseHelper _dbHelper;

  const DistributorStockLocalDatasource(this._dbHelper);

  /// Atomically clears the distributor_stocks table and inserts [stocks].
  /// Full-replace mirrors the products sync pattern — no incremental deltas needed
  /// since the entire stock snapshot is small and changes frequently.
  Future<void> replaceAll(List<DistributorStockModel> stocks) async {
    final db = await _dbHelper.database;
    await db.transaction((txn) async {
      await txn.delete('distributor_stocks');
      if (stocks.isEmpty) return;
      final batch = txn.batch();
      for (final s in stocks) {
        batch.insert(
          'distributor_stocks',
          s.toMap(),
          conflictAlgorithm: ConflictAlgorithm.replace,
        );
      }
      await batch.commit(noResult: true);
    });
  }

  Future<DateTime?> getLastSyncedAt() async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'metadata',
      where: 'key = ?',
      whereArgs: ['distributor_stocks_last_synced_at'],
    );
    if (rows.isEmpty) return null;
    return DateTime.tryParse(rows.first['value'] as String);
  }

  Future<void> saveLastSyncedAt(DateTime dt) async {
    final db = await _dbHelper.database;
    await db.insert(
      'metadata',
      {
        'key': 'distributor_stocks_last_synced_at',
        'value': dt.toIso8601String(),
      },
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  Future<int> getStockItemCount() async {
    final db = await _dbHelper.database;
    final result = await db.rawQuery('SELECT COUNT(*) AS c FROM distributor_stocks');
    return (result.first['c'] as int?) ?? 0;
  }

  /// All stock rows joined with product code + description. Used by the
  /// stock catalog page. Ordered by product code then stock type.
  Future<List<StockWithProduct>> getAllWithProductInfo() async {
    final db = await _dbHelper.database;
    final rows = await db.rawQuery('''
      SELECT ds.product_id, ds.stock_type, ds.quantity_on_hand, ds.last_updated_at,
             p.code AS product_code, p.item_description AS product_name
      FROM distributor_stocks ds
      LEFT JOIN products p ON p.id = ds.product_id
      ORDER BY p.code ASC, ds.stock_type ASC
    ''');
    return rows.map(StockWithProduct._fromRow).toList();
  }

  /// Returns the Normal stock quantity for [productId], or null if not synced.
  Future<double?> getNormalStockForProduct(int productId) async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'distributor_stocks',
      columns: ['quantity_on_hand'],
      where: 'product_id = ? AND stock_type = ?',
      whereArgs: [productId, 'Normal'],
      limit: 1,
    );
    if (rows.isEmpty) return null;
    return (rows.first['quantity_on_hand'] as num).toDouble();
  }
}
