import 'package:path/path.dart';
import 'package:sqflite/sqflite.dart';

/// Singleton SQLite database for local offline storage.
/// Tables are created in [_onCreate]; increment [_dbVersion] + add
/// [onUpgrade] when schema changes.
class DatabaseHelper {
  static const _dbName = 'sfa_local.db';
  static const _dbVersion = 4;

  DatabaseHelper._private();
  static final DatabaseHelper instance = DatabaseHelper._private();

  Database? _database;

  Future<Database> get database async {
    _database ??= await _initDatabase();
    return _database!;
  }

  Future<Database> _initDatabase() async {
    final path = join(await getDatabasesPath(), _dbName);
    return openDatabase(
      path,
      version: _dbVersion,
      onCreate: _onCreate,
      onUpgrade: _onUpgrade,
    );
  }

  Future<void> _onCreate(Database db, int version) async {
    // Products catalog — full replace on every sync
    await db.execute('''
      CREATE TABLE products (
        id              INTEGER PRIMARY KEY,
        code            TEXT    NOT NULL,
        item_description TEXT   NOT NULL,
        print_description TEXT,
        pieces_per_pack INTEGER NOT NULL,
        image_url       TEXT
      )
    ''');

    // Generic key-value store for sync timestamps and app-level metadata
    await db.execute('''
      CREATE TABLE metadata (
        key   TEXT PRIMARY KEY,
        value TEXT NOT NULL
      )
    ''');

    await _createDailyOutletsTable(db);
    await _createPricingStructuresTable(db);
    await _createPricingItemsTable(db);
  }

  Future<void> _onUpgrade(Database db, int oldVersion, int newVersion) async {
    if (oldVersion < 2) await _createDailyOutletsTable(db);
    if (oldVersion < 3) await _createPricingItemsTable(db);
    if (oldVersion < 4) await _createPricingStructuresTable(db);
  }

  Future<void> _createPricingStructuresTable(Database db) async {
    await db.execute('''
      CREATE TABLE pricing_structures (
        id         INTEGER PRIMARY KEY,
        name       TEXT    NOT NULL,
        is_default INTEGER NOT NULL DEFAULT 0
      )
    ''');
  }

  Future<void> _createPricingItemsTable(Database db) async {
    await db.execute('''
      CREATE TABLE pricing_items (
        id                       INTEGER PRIMARY KEY,
        pricing_structure_id     INTEGER NOT NULL,
        product_id               INTEGER NOT NULL,
        product_code             TEXT    NOT NULL,
        product_item_description TEXT    NOT NULL,
        dealer_pack_price        REAL,
        dealer_case_price        REAL,
        promotional_price        REAL
      )
    ''');
  }

  Future<void> _createDailyOutletsTable(Database db) async {
    await db.execute('''
      CREATE TABLE daily_outlets (
        id               INTEGER PRIMARY KEY,
        name             TEXT    NOT NULL,
        address          TEXT    NOT NULL,
        tel              TEXT    NOT NULL,
        email            TEXT,
        contact_person   TEXT,
        latitude         REAL    NOT NULL,
        longitude        REAL    NOT NULL,
        outlet_type      TEXT    NOT NULL,
        outlet_category  TEXT    NOT NULL,
        route_id         INTEGER NOT NULL,
        route_name       TEXT    NOT NULL,
        is_active        INTEGER NOT NULL DEFAULT 1
      )
    ''');
  }
}
