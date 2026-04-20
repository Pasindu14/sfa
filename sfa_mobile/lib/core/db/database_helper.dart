import 'package:path/path.dart';
import 'package:sqflite/sqflite.dart';

/// Singleton SQLite database for local offline storage.
/// Tables are created in [_onCreate]; increment [_dbVersion] + add
/// [onUpgrade] when schema changes.
class DatabaseHelper {
  static const _dbName = 'sfa_local.db';
  static const _dbVersion = 1;

  DatabaseHelper._private();
  static final DatabaseHelper instance = DatabaseHelper._private();

  Database? _database;

  Future<Database> get database async {
    _database ??= await _initDatabase();
    return _database!;
  }

  Future<Database> _initDatabase() async {
    final path = join(await getDatabasesPath(), _dbName);
    return openDatabase(path, version: _dbVersion, onCreate: _onCreate);
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
  }
}
