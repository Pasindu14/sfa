import 'package:path/path.dart';
import 'package:sqflite/sqflite.dart';

/// Singleton SQLite database for local offline storage.
/// Tables are created in [_onCreate]; increment [_dbVersion] + add
/// [onUpgrade] when schema changes.
class DatabaseHelper {
  static const _dbName = 'sfa_local.db';
  static const _dbVersion = 5;

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
    await _createBillsTables(db);
  }

  Future<void> _onUpgrade(Database db, int oldVersion, int newVersion) async {
    if (oldVersion < 2) await _createDailyOutletsTable(db);
    if (oldVersion < 3) await _createPricingItemsTable(db);
    if (oldVersion < 4) await _createPricingStructuresTable(db);
    if (oldVersion < 5) await _createBillsTables(db);
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

  /// Offline outbox for bills created by the field rep.
  /// - [client_bill_id] is a client-generated UUID that doubles as the server-side
  ///   X-Idempotency-Key, so retries after a flaky connection never create duplicates.
  /// - [sync_status] drives the UI chip: pending (yellow), syncing, synced (green),
  ///   failed (red). last_sync_error carries the human-readable reason for failed rows
  ///   (concatenated per-product stock-out messages come from ApiError.fields).
  Future<void> _createBillsTables(Database db) async {
    await db.execute('''
      CREATE TABLE bills (
        client_bill_id       TEXT    PRIMARY KEY,
        outlet_id            INTEGER NOT NULL,
        billing_type         TEXT    NOT NULL,
        return_type          TEXT,
        original_bill_id     INTEGER,
        billing_date         TEXT    NOT NULL,
        bill_discount_rate   REAL    NOT NULL DEFAULT 0,
        sub_total_amount     REAL    NOT NULL,
        bill_discount_amount REAL    NOT NULL,
        total_amount         REAL    NOT NULL,
        notes                TEXT,
        created_at           TEXT    NOT NULL,
        sync_status          TEXT    NOT NULL,
        sync_attempts        INTEGER NOT NULL DEFAULT 0,
        last_sync_error      TEXT,
        last_sync_error_code TEXT,
        server_bill_id       INTEGER,
        server_bill_number   TEXT
      )
    ''');
    await db.execute('CREATE INDEX idx_bills_sync_status ON bills(sync_status)');
    await db.execute('CREATE INDEX idx_bills_outlet      ON bills(outlet_id)');

    await db.execute('''
      CREATE TABLE bill_items (
        id             INTEGER PRIMARY KEY AUTOINCREMENT,
        client_bill_id TEXT    NOT NULL,
        product_id     INTEGER NOT NULL,
        quantity       REAL    NOT NULL,
        unit_price     REAL    NOT NULL,
        discount_rate  REAL    NOT NULL DEFAULT 0,
        is_free_issue  INTEGER NOT NULL DEFAULT 0,
        line_number    INTEGER NOT NULL,
        FOREIGN KEY(client_bill_id) REFERENCES bills(client_bill_id) ON DELETE CASCADE
      )
    ''');
    await db.execute('CREATE INDEX idx_bill_items_bill ON bill_items(client_bill_id)');
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
