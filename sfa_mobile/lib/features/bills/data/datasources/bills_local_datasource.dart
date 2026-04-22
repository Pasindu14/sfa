import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/bills/data/models/bill_item_model.dart';
import 'package:uswatte/features/bills/data/models/bill_model.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';

/// A product row shaped for the Create Bill product picker — joined with the
/// default pricing structure so the UI can show a price without a second query.
class ProductWithPrice {
  final int id;
  final String code;
  final String itemDescription;
  final double? dealerPackPrice;
  final double? dealerCasePrice;
  final int packsPerCase;

  const ProductWithPrice({
    required this.id,
    required this.code,
    required this.itemDescription,
    this.dealerPackPrice,
    this.dealerCasePrice,
    this.packsPerCase = 1,
  });
}

class BillsLocalDatasource {
  final DatabaseHelper _dbHelper;

  const BillsLocalDatasource(this._dbHelper);

  // ── Bills CRUD ─────────────────────────────────────────────────────────────

  Future<void> insert(BillModel bill) async {
    final db = await _dbHelper.database;
    await db.transaction((txn) async {
      await txn.insert(
        'bills',
        bill.toMap(),
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
      for (final item in bill.items) {
        await txn.insert('bill_items', item.toMap());
      }
    });
  }

  Future<List<BillModel>> getAll({int? limit}) async {
    final db = await _dbHelper.database;
    final billRows = await db.query(
      'bills',
      orderBy: 'created_at DESC',
      limit: limit,
    );
    if (billRows.isEmpty) return [];

    final billIds = billRows.map((r) => r['client_bill_id'] as String).toList();
    final placeholders = List.filled(billIds.length, '?').join(',');
    final itemRows = await db.query(
      'bill_items',
      where: 'client_bill_id IN ($placeholders)',
      whereArgs: billIds,
      orderBy: 'client_bill_id ASC, line_number ASC',
    );

    final itemsByBill = <String, List<BillItemModel>>{};
    for (final row in itemRows) {
      final id = row['client_bill_id'] as String;
      itemsByBill.putIfAbsent(id, () => []).add(BillItemModel.fromMap(row));
    }
    return billRows
        .map((r) => BillModel.fromMap(r, itemsByBill[r['client_bill_id']] ?? []))
        .toList();
  }

  Future<BillModel?> getById(String clientBillId) async {
    final db = await _dbHelper.database;
    final billRows = await db.query(
      'bills',
      where: 'client_bill_id = ?',
      whereArgs: [clientBillId],
      limit: 1,
    );
    if (billRows.isEmpty) return null;
    final itemRows = await db.rawQuery(
      '''
      SELECT bi.*,
             p.item_description AS product_name
      FROM bill_items bi
      LEFT JOIN products p ON p.id = bi.product_id
      WHERE bi.client_bill_id = ?
      ORDER BY bi.line_number ASC
      ''',
      [clientBillId],
    );
    return BillModel.fromMap(
      billRows.first,
      itemRows.map(BillItemModel.fromMap).toList(),
    );
  }

  Future<List<BillModel>> getPendingForSync() async {
    final db = await _dbHelper.database;
    final billRows = await db.query(
      'bills',
      where: "sync_status IN ('pending', 'failed')",
      orderBy: 'created_at ASC',
    );
    if (billRows.isEmpty) return [];

    final billIds = billRows.map((r) => r['client_bill_id'] as String).toList();
    final placeholders = List.filled(billIds.length, '?').join(',');
    final itemRows = await db.query(
      'bill_items',
      where: 'client_bill_id IN ($placeholders)',
      whereArgs: billIds,
      orderBy: 'client_bill_id ASC, line_number ASC',
    );
    final itemsByBill = <String, List<BillItemModel>>{};
    for (final row in itemRows) {
      final id = row['client_bill_id'] as String;
      itemsByBill.putIfAbsent(id, () => []).add(BillItemModel.fromMap(row));
    }
    return billRows
        .map((r) => BillModel.fromMap(r, itemsByBill[r['client_bill_id']] ?? []))
        .toList();
  }

  Future<int> countPendingOrFailed() async {
    final db = await _dbHelper.database;
    final rows = await db.rawQuery(
      "SELECT COUNT(*) AS c FROM bills WHERE sync_status IN ('pending', 'failed')",
    );
    return (rows.first['c'] as int?) ?? 0;
  }

  Future<void> markSyncing(String clientBillId) async {
    final db = await _dbHelper.database;
    await db.update(
      'bills',
      {'sync_status': SyncStatus.syncing.dbValue},
      where: 'client_bill_id = ?',
      whereArgs: [clientBillId],
    );
  }

  Future<void> markSynced(
    String clientBillId, {
    required int serverBillId,
    required String serverBillNumber,
  }) async {
    final db = await _dbHelper.database;
    await db.update(
      'bills',
      {
        'sync_status': SyncStatus.synced.dbValue,
        'server_bill_id': serverBillId,
        'server_bill_number': serverBillNumber,
        'last_sync_error': null,
        'last_sync_error_code': null,
      },
      where: 'client_bill_id = ?',
      whereArgs: [clientBillId],
    );
  }

  Future<void> markFailed(
    String clientBillId, {
    required String errorCode,
    required String errorMessage,
  }) async {
    final db = await _dbHelper.database;
    await db.rawUpdate(
      '''UPDATE bills
         SET sync_status = ?,
             sync_attempts = sync_attempts + 1,
             last_sync_error_code = ?,
             last_sync_error = ?
         WHERE client_bill_id = ?''',
      [SyncStatus.failed.dbValue, errorCode, errorMessage, clientBillId],
    );
  }

  /// Network error → keep pending but bump attempts so the UI can show them.
  Future<void> markPendingAfterNetworkError(String clientBillId) async {
    final db = await _dbHelper.database;
    await db.rawUpdate(
      '''UPDATE bills
         SET sync_status = ?,
             sync_attempts = sync_attempts + 1
         WHERE client_bill_id = ?''',
      [SyncStatus.pending.dbValue, clientBillId],
    );
  }

  Future<void> delete(String clientBillId) async {
    final db = await _dbHelper.database;
    await db.delete(
      'bills',
      where: 'client_bill_id = ? AND sync_status != ?',
      whereArgs: [clientBillId, SyncStatus.synced.dbValue],
    );
  }

  /// Deletes synced bills older than [cutoff].
  ///
  /// Deliberately narrow — ONLY rows with sync_status = 'synced' are touched.
  /// Pending / failed / syncing rows are never purged regardless of age, since
  /// they represent unfinished sales that must not be silently lost.
  ///
  /// sqflite doesn't enforce FK cascades, so bill_items are deleted explicitly
  /// inside the same transaction.
  Future<int> purgeSyncedOlderThan(DateTime cutoff) async {
    final db = await _dbHelper.database;
    final cutoffIso = cutoff.toUtc().toIso8601String();
    int rowsDeleted = 0;
    await db.transaction((txn) async {
      final staleIds = await txn.query(
        'bills',
        columns: ['client_bill_id'],
        where: 'sync_status = ? AND created_at < ?',
        whereArgs: [SyncStatus.synced.dbValue, cutoffIso],
      );
      if (staleIds.isEmpty) return;

      final ids = staleIds.map((r) => r['client_bill_id'] as String).toList();
      final placeholders = List.filled(ids.length, '?').join(',');

      await txn.delete(
        'bill_items',
        where: 'client_bill_id IN ($placeholders)',
        whereArgs: ids,
      );
      rowsDeleted = await txn.delete(
        'bills',
        where: 'client_bill_id IN ($placeholders)',
        whereArgs: ids,
      );
    });
    return rowsDeleted;
  }

  // ── Product search for the Create Bill picker ─────────────────────────────

  /// Searches `products` by code OR description prefix, joined with the default
  /// pricing_structures entry for the dealer-pack price. Capped at 50 rows to
  /// keep the dropdown snappy.
  Future<List<ProductWithPrice>> searchProducts(
    String query, {
    int limit = 50,
    int? pricingStructureId,
  }) async {
    final db = await _dbHelper.database;
    final q = '%${query.trim()}%';
    // If a specific structure is given, join on it directly.
    // Otherwise fall back to the default structure so the call is always safe.
    final structureClause = pricingStructureId != null
        ? 'pi.pricing_structure_id = $pricingStructureId'
        : 'pi.pricing_structure_id = (SELECT id FROM pricing_structures WHERE is_default = 1 LIMIT 1)';
    final rows = await db.rawQuery(
      '''
      SELECT p.id, p.code, p.item_description,
             p.pieces_per_pack AS packs_per_case,
             pi.dealer_pack_price AS dealer_pack_price,
             pi.dealer_case_price AS dealer_case_price
      FROM products p
      LEFT JOIN pricing_items pi
        ON pi.product_id = p.id
       AND $structureClause
      WHERE p.code LIKE ? OR p.item_description LIKE ?
      ORDER BY p.code ASC
      LIMIT ?
      ''',
      [q, q, limit],
    );
    return rows
        .map((r) => ProductWithPrice(
              id: r['id'] as int,
              code: r['code'] as String,
              itemDescription: r['item_description'] as String,
              dealerPackPrice: (r['dealer_pack_price'] as num?)?.toDouble(),
              dealerCasePrice: (r['dealer_case_price'] as num?)?.toDouble(),
              packsPerCase: (r['packs_per_case'] as int?) ?? 1,
            ))
        .toList();
  }
}
