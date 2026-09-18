import 'package:sqflite/sqflite.dart';
import 'package:uswatte/core/db/database_helper.dart';
import 'package:uswatte/features/pricing/data/models/pricing_structure_model.dart';

/// Local cache of the active pricing structures (`price_structures`) and their
/// per-product prices (`price_structure_items`). Fully replaced on every sync.
class PricingLocalDatasource {
  static const _lastSyncedKey = 'price_structures_last_synced_at';

  final DatabaseHelper _dbHelper;

  const PricingLocalDatasource(this._dbHelper);

  /// Every cached structure (without its items), default first.
  Future<List<PricingStructureModel>> getAllStructures() async {
    final db = await _dbHelper.database;
    final rows = await db.rawQuery('''
      SELECT s.id, s.name, s.is_default,
             COUNT(i.product_id) AS item_count
      FROM price_structures s
      LEFT JOIN price_structure_items i
        ON i.structure_id = s.id
      GROUP BY s.id, s.name, s.is_default
      ORDER BY s.is_default DESC, s.name ASC
    ''');
    return rows.map(PricingStructureModel.fromMap).toList();
  }

  /// The products priced in one cached structure, by code. Backs the Sync
  /// page's price-list viewer.
  Future<List<PriceListItem>> getStructureItems(int structureId) async {
    final db = await _dbHelper.database;
    final rows = await db.rawQuery('''
      SELECT p.id AS product_id, p.code, p.item_description, p.pieces_per_pack,
             i.dealer_pack_price, i.dealer_case_price, i.mrp
      FROM price_structure_items i
      JOIN products p ON p.id = i.product_id
      WHERE i.structure_id = ?
      ORDER BY p.code ASC
    ''', [structureId]);
    return rows
        .map((r) => PriceListItem(
              productId: r['product_id'] as int,
              code: r['code'] as String,
              description: r['item_description'] as String,
              packsPerCase: (r['pieces_per_pack'] as int?) ?? 0,
              packPrice: (r['dealer_pack_price'] as num).toDouble(),
              casePrice: (r['dealer_case_price'] as num?)?.toDouble(),
              mrp: (r['mrp'] as num?)?.toDouble(),
            ))
        .toList();
  }

  /// Atomically clears both tables and inserts [structures] with their items.
  Future<void> replaceAll(List<PricingStructureModel> structures) async {
    final db = await _dbHelper.database;
    await db.transaction((txn) async {
      await txn.delete('price_structure_items');
      await txn.delete('price_structures');
      if (structures.isEmpty) return;
      final batch = txn.batch();
      for (final s in structures) {
        batch.insert('price_structures', s.toMap());
        for (final item in s.items) {
          batch.insert(
            'price_structure_items',
            item.toMap(),
            // A product repeated inside one structure must not abort the whole
            // replace; the later row wins.
            conflictAlgorithm: ConflictAlgorithm.replace,
          );
        }
      }
      await batch.commit(noResult: true);
    });
  }

  /// True when at least one structure is cached. A conditional GET is only
  /// safe against a non-empty cache — a 304 would otherwise leave the rep with
  /// no price lists.
  Future<bool> hasAny() async {
    final db = await _dbHelper.database;
    final rows = await db.rawQuery('SELECT 1 FROM price_structures LIMIT 1');
    return rows.isNotEmpty;
  }

  Future<DateTime?> getLastSyncedAt() async {
    final db = await _dbHelper.database;
    final rows = await db.query(
      'metadata',
      where: 'key = ?',
      whereArgs: [_lastSyncedKey],
    );
    if (rows.isEmpty) return null;
    return DateTime.tryParse(rows.first['value'] as String);
  }

  Future<void> saveLastSyncedAt(DateTime dt) async {
    final db = await _dbHelper.database;
    await db.insert(
      'metadata',
      {'key': _lastSyncedKey, 'value': dt.toIso8601String()},
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }
}

/// One product's prices inside one price list. [casePrice] null means the
/// list leaves it to pack × packs per case (see [effectiveCasePrice]).
class PriceListItem {
  final int productId;
  final String code;
  final String description;
  final int packsPerCase;
  final double packPrice;
  final double? casePrice;
  final double? mrp;

  const PriceListItem({
    required this.productId,
    required this.code,
    required this.description,
    required this.packsPerCase,
    required this.packPrice,
    this.casePrice,
    this.mrp,
  });

  bool get isCaseDerived => casePrice == null || casePrice! <= 0;

  /// The case price a bill actually uses — mirrors the quantity dialog.
  double? get effectiveCasePrice {
    if (!isCaseDerived) return casePrice;
    return packsPerCase > 0 ? packPrice * packsPerCase : null;
  }
}
