/**
* Migration script: SQL Server AreaMaster → PostgreSQL Territories
*
* Mapping:
*   AreaCode   → Id          (preserve exact ID — CRITICAL)
*   AreaCode   → AreaId      (same value — each area becomes its own territory)
*   AreaName   → Name
*   EntryDate  → CreatedAt, UpdatedAt
*   RegionId   → hardcoded DEFAULT_REGION_ID
*   CreatedBy  → hardcoded DEFAULT_USER_ID
*   IsActive   → hardcoded true
*
* Run: npx tsx scripts/migrate-territories.ts
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
const DEFAULT_REGION_ID = 1
const DEFAULT_USER_ID = 3
const BATCH_SIZE = 500

// ── Types ──────────────────────────────────────────────────────────────────
interface AreaMasterRow {
  AreaCode: number
  AreaName: string
  EntryDate: Date | null
}

interface MappedRow {
  id: number
  name: string
  areaId: number
  createdAt: Date
}

// ── Helpers ────────────────────────────────────────────────────────────────

/** Build a multi-row INSERT with parameterized placeholders for a batch of rows.
 *  Each row has 9 columns → placeholders are $1..$9, $10..$18, etc.
 */
function buildBatchInsert(batch: MappedRow[]): { text: string; values: unknown[] } {
  const COLS_PER_ROW = 9
  const values: unknown[] = []
  const rowPlaceholders: string[] = []

  batch.forEach((row, i) => {
    const base = i * COLS_PER_ROW
    rowPlaceholders.push(
      `($${base + 1}, $${base + 2}, $${base + 3}, $${base + 4}, $${base + 5}, $${base + 6}, $${base + 7}, $${base + 8}, $${base + 9})`
    )
    values.push(
      row.id,
      row.name,
      row.areaId,
      DEFAULT_REGION_ID,
      true,
      row.createdAt,
      row.createdAt,
      DEFAULT_USER_ID,
      DEFAULT_USER_ID
    )
  })

  const text = `
    INSERT INTO "Territories" ("Id", "Name", "AreaId", "RegionId", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
    VALUES ${rowPlaceholders.join(', ')}
    ON CONFLICT ("Id") DO UPDATE SET
      "Name"      = EXCLUDED."Name",
      "UpdatedAt" = EXCLUDED."UpdatedAt"
  `
  return { text, values }
}

// ── Main ───────────────────────────────────────────────────────────────────
async function migrate() {
  console.log('Connecting to SQL Server...')
  const sqlPool = await sql.connect(sqlConfig)
  console.log('Connected to SQL Server ✓')

  // 1. Read all rows from SQL Server
  const result = await sqlPool.request().query<AreaMasterRow>(`
    SELECT AreaCode, AreaName, EntryDate
    FROM [SeefaUswattaSFABiscut].[dbo].[AreaMaster]
    ORDER BY AreaCode
  `)
  console.log(`Found ${result.recordset.length} rows in AreaMaster`)

  // 2. Transform — filter invalid rows, map columns
  const validRows: MappedRow[] = []
  let skipped = 0

  for (const row of result.recordset) {
    const name = row.AreaName?.trim()
    if (!row.AreaCode || !name) {
      console.warn(`  ⚠ Skipping row — null/invalid fields (AreaCode=${row.AreaCode}, Name=${name})`)
      skipped++
      continue
    }
    validRows.push({
      id: row.AreaCode,
      name,
      areaId: row.AreaCode,   // AreaCode is both the Territory Id and its AreaId
      createdAt: row.EntryDate ? new Date(row.EntryDate) : new Date(),
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

    // Reset sequence so future app inserts don't collide with migrated IDs
    await pgClient.query(`
      SELECT setval(
        pg_get_serial_sequence('"Territories"', 'Id'),
        (SELECT MAX("Id") FROM "Territories")
      )
    `)

    await pgClient.query('COMMIT')
    console.log(`\nDone! Inserted/updated: ${inserted}, Skipped: ${skipped}`)
    console.log('Sequence reset to MAX(Id) ✓')
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
