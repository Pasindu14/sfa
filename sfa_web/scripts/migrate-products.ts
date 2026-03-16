/**
 * Migration script: SQL Server ItemMaster → PostgreSQL Products
 *
 * SQL Server source columns → PostgreSQL Products columns:
 *   ItemCode          → Code             (unique product code)
 *   ItemDescription   → ItemDescription  (full display name)
 *   PrintDescription  → PrintDescription (label for print/reports)
 *   PiecesforPack     → PiecesPerPack    (units per pack)
 *   ItemImage         → ImageUrl         (storage path or URL)
 *   Remarks           → Remarks
 *   Active            → IsActive         (all set to true per migration defaults)
 *   EntryDate         → CreatedAt, UpdatedAt (fallback: today)
 *   (UserId)          → CreatedBy, UpdatedBy (hardcoded DEFAULT_USER_ID = 2)
 *
 * Dropped columns (no matching column in Products):
 *   PartNo, ReorderQuantity, OrderQty, RackNo, MeasureCode,
 *   StockIn*, LastPurchasePrice, AvaragePurchasePrice,
 *   WholesalePrice, RetailPrice, SpecialPrice, SpecialPriceQty,
 *   ItemOrder, CategoryCode, Make, Model, StockLocation,
 *   DiscountGroup, PartCode, MaximumStock, ItemType,
 *   LastPurchaseDate, LastIssuesDate, LastIssuesPrice,
 *   LastUpdateDate, ItemImageSmall, ItemSmall, OldPrice,
 *   ReorderLevel, IncAccountCode, IsRebateItem, RebateAmount,
 *   PromotionalItem
 *
 * Run: npx tsx scripts/migrate-products.ts
 */

import * as sql from 'mssql'
import { Pool } from 'pg'

const API_CONNECTION_STRING = 'postgresql://neondb_owner:npg_ScokZ26VKDRW@ep-holy-forest-a1q8udg7-pooler.ap-southeast-1.aws.neon.tech/SFA?sslmode=require&channel_binding=require'

// ── SQL Server config (local) ──────────────────────────────────────────────
const sqlConfig: sql.config = {
  server: 'localhost',
  database: 'SeefaUswattaSFABiscut',
  options: {
    trustServerCertificate: true,
    encrypt: false,
  },
  user: 'sa',
  password: 'Migration@123',
}

// ── PostgreSQL config (Neon cloud — SFA database used by the API) ──────────
const pg = new Pool({
  connectionString: API_CONNECTION_STRING,
  ssl: { rejectUnauthorized: false },
})

// ── Migration defaults ─────────────────────────────────────────────────────
const DEFAULT_USER_ID = 3
const DEFAULT_DATE = new Date()
const BATCH_SIZE = 500

// ── Types ──────────────────────────────────────────────────────────────────
interface ItemMasterRow {
  ItemCode: string
  ItemDescription: string
  PrintDescription: string | null
  PiecesforPack: number | null
  ItemImage: string | null
  Remarks: string | null
  Active: boolean | null
  EntryDate: Date | null
}

interface MappedRow {
  code: string
  itemDescription: string
  printDescription: string | null
  piecesPerPack: number
  imageUrl: string | null
  remarks: string | null
  createdAt: Date
}

// ── Helpers ────────────────────────────────────────────────────────────────

/** Build a multi-row INSERT with parameterized placeholders for a batch of rows.
 *  Each row has 10 columns → placeholders are $1..$10, $11..$20, etc.
 *  Id is excluded — PostgreSQL identity column auto-generates it.
 */
function buildBatchInsert(batch: MappedRow[]): { text: string; values: unknown[] } {
  const COLS_PER_ROW = 10
  const values: unknown[] = []
  const rowPlaceholders: string[] = []

  batch.forEach((row, i) => {
    const base = i * COLS_PER_ROW
    rowPlaceholders.push(
      `($${base + 1}, $${base + 2}, $${base + 3}, $${base + 4}, $${base + 5}, $${base + 6}, $${base + 7}, $${base + 8}, $${base + 9}, $${base + 10})`
    )
    values.push(
      row.code,
      row.itemDescription,
      row.printDescription,
      row.piecesPerPack,
      row.imageUrl,
      row.remarks,
      true,             // IsActive — all migrated products are active
      row.createdAt,    // CreatedAt
      row.createdAt,    // UpdatedAt
      DEFAULT_USER_ID   // CreatedBy & UpdatedBy share the same default
    )
  })

  const text = `
    INSERT INTO "Products" ("Code", "ItemDescription", "PrintDescription", "PiecesPerPack", "ImageUrl", "Remarks", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy")
    VALUES ${rowPlaceholders.join(', ')}
    ON CONFLICT ("Code") DO UPDATE SET
      "ItemDescription"  = EXCLUDED."ItemDescription",
      "PrintDescription" = EXCLUDED."PrintDescription",
      "PiecesPerPack"    = EXCLUDED."PiecesPerPack",
      "ImageUrl"         = EXCLUDED."ImageUrl",
      "Remarks"          = EXCLUDED."Remarks",
      "IsActive"         = EXCLUDED."IsActive",
      "UpdatedAt"        = EXCLUDED."UpdatedAt"
  `
  return { text, values }
}

// ── Main ───────────────────────────────────────────────────────────────────
async function migrate() {
  console.log('Connecting to SQL Server...')
  const sqlPool = await sql.connect(sqlConfig)
  console.log('Connected to SQL Server ✓')

  // 1. Read all rows from SQL Server
  const result = await sqlPool.request().query<ItemMasterRow>(`
    SELECT
      ItemCode,
      ItemDescription,
      PrintDescription,
      PiecesforPack,
      ItemImage,
      Remarks,
      Active,
      EntryDate
    FROM [SeefaUswattaSFABiscut].[dbo].[ItemMaster]
    ORDER BY ItemCode
  `)
  console.log(`Found ${result.recordset.length} rows in ItemMaster`)

  // 2. Transform — filter invalid rows, map columns
  const validRows: MappedRow[] = []
  let skipped = 0

  for (const row of result.recordset) {
    const code = row.ItemCode?.trim()
    const description = row.ItemDescription?.trim()

    if (!code || !description) {
      console.warn(`  ⚠ Skipping row — null/invalid fields (ItemCode=${row.ItemCode}, Description=${description})`)
      skipped++
      continue
    }

    validRows.push({
      code,
      itemDescription: description,
      printDescription: row.PrintDescription?.trim() || null,
      piecesPerPack: row.PiecesforPack ?? 0,
      imageUrl: row.ItemImage?.trim() || null,
      remarks: row.Remarks?.trim() || null,
      createdAt: row.EntryDate ? new Date(row.EntryDate) : DEFAULT_DATE,
    })
  }

  console.log(`Valid rows to insert: ${validRows.length} (skipped: ${skipped})\n`)

  // 3. Insert into PostgreSQL in batches
  console.log('Connecting to PostgreSQL...')
  const pgClient = await pg.connect()
  console.log('Connected to PostgreSQL ✓\n')

  await pgClient.query('BEGIN')

  try {
    let inserted = 0
    const totalBatches = Math.ceil(validRows.length / BATCH_SIZE)

    for (let i = 0; i < validRows.length; i += BATCH_SIZE) {
      const batch = validRows.slice(i, i + BATCH_SIZE)
      const batchNum = Math.floor(i / BATCH_SIZE) + 1
      const { text, values } = buildBatchInsert(batch)

      await pgClient.query(text, values)
      inserted += batch.length
      console.log(`  Batch ${batchNum}/${totalBatches} → inserted ${inserted}/${validRows.length} rows`)
    }

    await pgClient.query('COMMIT')
    console.log(`\nDone! Inserted/updated: ${inserted}, Skipped: ${skipped}`)
  } catch (err) {
    await pgClient.query('ROLLBACK')
    console.error('\nMigration failed — rolled back:', err)
    throw err
  } finally {
    pgClient.release()
  }

  await sqlPool.close()
  await pg.end()
}

migrate().catch((err) => {
  console.error(err)
  process.exit(1)
})
