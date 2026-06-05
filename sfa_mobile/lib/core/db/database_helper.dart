import 'package:path/path.dart';
import 'package:sqflite/sqflite.dart';

/// Singleton SQLite database for local offline storage.
/// Tables are created in [_onCreate]; increment [_dbVersion] + add
/// [onUpgrade] when schema changes.
class DatabaseHelper {
  static const _dbName = 'sfa_local.db';
  static const _dbVersion = 16;

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
    // Product categories — full replace on every sync
    await db.execute('''
      CREATE TABLE product_categories (
        id         INTEGER PRIMARY KEY,
        name       TEXT    NOT NULL,
        sort_order INTEGER NOT NULL DEFAULT 0
      )
    ''');

    // Products catalog — full replace on every sync
    await db.execute('''
      CREATE TABLE products (
        id               INTEGER PRIMARY KEY,
        code             TEXT    NOT NULL,
        item_description TEXT    NOT NULL,
        print_description TEXT,
        pieces_per_pack  INTEGER NOT NULL,
        image_url        TEXT,
        category_id      INTEGER,
        dealer_pack_price REAL   NOT NULL DEFAULT 0,
        dealer_case_price REAL   NOT NULL DEFAULT 0,
        mrp              REAL    NOT NULL DEFAULT 0
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
    await _createBillsTables(db);
    await _createNotBillingsTable(db);
    await _createDistributorStocksTable(db);
  }

  Future<void> _onUpgrade(Database db, int oldVersion, int newVersion) async {
    if (oldVersion < 2) await _createDailyOutletsTable(db);
    // v3/v4 created the now-removed pricing tables; v16 tears them down anyway,
    // so we skip creating them on the way up from very old installs.
    if (oldVersion < 5) await _createBillsTables(db);
    if (oldVersion < 6) await _migrateBillItemsV6(db);
    if (oldVersion < 7) await _createNotBillingsTable(db);
    if (oldVersion < 8) await _migrateNotBillingsV8(db);
    if (oldVersion < 9) await _migrateBillsV9(db);
    if (oldVersion < 10) await _migrateProductCategoriesV10(db);
    if (oldVersion < 11) await _migrateBillsV11(db);
    if (oldVersion < 12) await _migrateBillItemsV12(db);
    if (oldVersion < 13) await _migrateBillItemsV13(db);
    if (oldVersion < 14) await _createDistributorStocksTable(db);
    if (oldVersion < 15) await _migrateOutletsV15(db);
    if (oldVersion < 16) await _migrateProductPricesAndDropPricingV16(db);
  }

  /// Prices moved onto the product itself — add the columns — and the
  /// PricingStructures feature was removed, so drop its cache tables and the
  /// sync marker. DROP ... IF EXISTS keeps this safe for installs that never
  /// created the pricing tables.
  Future<void> _migrateProductPricesAndDropPricingV16(Database db) async {
    await db.execute(
        'ALTER TABLE products ADD COLUMN dealer_pack_price REAL NOT NULL DEFAULT 0');
    await db.execute(
        'ALTER TABLE products ADD COLUMN dealer_case_price REAL NOT NULL DEFAULT 0');
    await db.execute(
        'ALTER TABLE products ADD COLUMN mrp REAL NOT NULL DEFAULT 0');
    await db.execute('DROP TABLE IF EXISTS pricing_items');
    await db.execute('DROP TABLE IF EXISTS pricing_structures');
    await db.execute("DELETE FROM metadata WHERE key = 'pricing_last_synced_at'");
  }

  /// Adds free_issue_source to bill_items so the rep can flag whether each
  /// FOC line is funded By Company (default — drawn from the FOC stock pool)
  /// or By Distributor (drawn from the distributor's normal saleable stock).
  /// Backfills every existing FOC row as 'Company' since that was the only
  /// behaviour the system supported before this column existed.
  Future<void> _migrateBillItemsV13(Database db) async {
    await db.execute(
        'ALTER TABLE bill_items ADD COLUMN free_issue_source TEXT');
    await db.execute('''
      UPDATE bill_items
         SET free_issue_source = 'Company'
       WHERE billing_item_type = 'FreeIssue'
         AND free_issue_source IS NULL
    ''');
  }

  /// Promote legacy (is_free_issue = 1) rows to billing_item_type = 'FreeIssue'.
  /// The is_free_issue column is left in place (SQLite < 3.35 cannot DROP COLUMN);
  /// the model no longer writes it on new rows.
  Future<void> _migrateBillItemsV12(Database db) async {
    await db.execute('''
      UPDATE bill_items
         SET billing_item_type = 'FreeIssue'
       WHERE is_free_issue = 1
         AND billing_item_type = 'Sale'
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
        latitude             REAL,
        longitude            REAL,
        outlet_name          TEXT,
        outlet_category      TEXT,
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

    // NOTE: is_free_issue is intentionally omitted on fresh installs.
    // Existing devices keep the column (migration cannot drop it on older SQLite),
    // but the app no longer reads/writes it. billing_item_type = 'FreeIssue' is the truth.
    await db.execute('''
      CREATE TABLE bill_items (
        id                INTEGER PRIMARY KEY AUTOINCREMENT,
        client_bill_id    TEXT    NOT NULL,
        product_id        INTEGER NOT NULL,
        quantity          REAL    NOT NULL,
        unit_price        REAL    NOT NULL,
        discount_rate     REAL    NOT NULL DEFAULT 0,
        billing_item_type TEXT    NOT NULL DEFAULT 'Sale',
        return_type       TEXT,
        free_issue_source TEXT,
        expire_date       TEXT,
        line_number       INTEGER NOT NULL,
        FOREIGN KEY(client_bill_id) REFERENCES bills(client_bill_id) ON DELETE CASCADE
      )
    ''');
    await db.execute('CREATE INDEX idx_bill_items_bill ON bill_items(client_bill_id)');
  }

  Future<void> _migrateBillItemsV6(Database db) async {
    await db.execute(
        "ALTER TABLE bill_items ADD COLUMN billing_item_type TEXT NOT NULL DEFAULT 'Sale'");
    await db.execute('ALTER TABLE bill_items ADD COLUMN return_type TEXT');
    await db.execute('ALTER TABLE bill_items ADD COLUMN expire_date TEXT');
  }

  /// Offline outbox for not-billing records created by the field rep.
  /// Mirrors the bills outbox pattern: UUID as idempotency key, sync state machine.
  Future<void> _createNotBillingsTable(Database db) async {
    await db.execute('''
      CREATE TABLE not_billings (
        client_not_billing_id  TEXT    PRIMARY KEY,
        outlet_id              INTEGER NOT NULL,
        outlet_name            TEXT,
        route_name             TEXT,
        not_billing_date       TEXT    NOT NULL,
        reason                 TEXT    NOT NULL,
        notes                  TEXT,
        created_at             TEXT    NOT NULL,
        sync_status            TEXT    NOT NULL,
        sync_attempts          INTEGER NOT NULL DEFAULT 0,
        last_sync_error        TEXT,
        last_sync_error_code   TEXT,
        server_not_billing_id     INTEGER,
        server_not_billing_number TEXT
      )
    ''');
    await db.execute('CREATE INDEX idx_not_billings_sync_status ON not_billings(sync_status)');
    await db.execute('CREATE INDEX idx_not_billings_outlet ON not_billings(outlet_id)');
  }

  Future<void> _migrateBillsV11(Database db) async {
    await db.execute('ALTER TABLE bills ADD COLUMN outlet_name TEXT');
    await db.execute('ALTER TABLE bills ADD COLUMN outlet_category TEXT');
  }

  Future<void> _migrateBillsV9(Database db) async {
    await db.execute('ALTER TABLE bills ADD COLUMN latitude REAL');
    await db.execute('ALTER TABLE bills ADD COLUMN longitude REAL');
  }

  Future<void> _migrateProductCategoriesV10(Database db) async {
    await db.execute('''
      CREATE TABLE product_categories (
        id         INTEGER PRIMARY KEY,
        name       TEXT    NOT NULL,
        sort_order INTEGER NOT NULL DEFAULT 0
      )
    ''');
    await db.execute('ALTER TABLE products ADD COLUMN category_id INTEGER');
  }

  Future<void> _migrateNotBillingsV8(Database db) async {
    await db.execute('ALTER TABLE not_billings ADD COLUMN outlet_name TEXT');
    await db.execute('ALTER TABLE not_billings ADD COLUMN route_name TEXT');
  }

  /// Stock levels for the rep's assigned distributor — full replace on every sync.
  /// Composite primary key on (product_id, stock_type) mirrors the server uniqueness constraint.
  Future<void> _createDistributorStocksTable(Database db) async {
    await db.execute('''
      CREATE TABLE distributor_stocks (
        product_id       INTEGER NOT NULL,
        stock_type       TEXT    NOT NULL,
        quantity_on_hand REAL    NOT NULL DEFAULT 0,
        last_updated_at  TEXT    NOT NULL,
        PRIMARY KEY (product_id, stock_type)
      )
    ''');
  }

  Future<void> _migrateOutletsV15(Database db) async {
    await db.execute(
        'ALTER TABLE daily_outlets ADD COLUMN last_bill_date TEXT');
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
        is_active        INTEGER NOT NULL DEFAULT 1,
        last_bill_date   TEXT
      )
    ''');
  }
}
