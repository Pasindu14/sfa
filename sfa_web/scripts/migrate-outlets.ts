/**
 * Migration script: SQL Server CustomerMaster → PostgreSQL Outlets
 *
 * Mapping:
 *   CustomerCode     → Id            (preserve exact ID — CRITICAL)
 *   CustomerName     → Name
 *   Address          → Address
 *   Tel              → Tel
 *   Email            → Email
 *   ContactName      → ContactPerson
 *   NicNo            → NicNo          (null/duplicate → 'LEGACY-<CustomerCode>')
 *   VatNo            → VatNo
 *   CreditLimit      → CreditLimit
 *   CustomerLocation → Latitude + Longitude  (format: 'lat,lng')
 *   DOB              → OwnerDOB
 *   remarks + Others → Remarks        (merged)
 *   Image            → Image
 *   OutletTypeCode   → OutletType     (enum: Small=0, Medium=1, Large=2)
 *   OutletCat        → OutletCategory (enum: Wholesale=0, SMMT=1)
 *   BillingPriceType → BillingPriceType (enum: DealerPrice=0, OldPrice=1, MarketPrice=2)
 *   ProvinceCode     → ProvinceCode
 *   DistrictCode     → DistrictCode
 *   RootCode         → RouteId + DivisionId + TerritoryId + AreaId + RegionId
 *                      (row skipped if RootCode not found in Routes)
 *   Active           → IsActive       (1 → true)
 *   EntryDate        → CreatedAt
 *   LastUpdateDate   → UpdatedAt
 *   UserId           → CreatedBy
 *   LastUpdateUser   → UpdatedBy
 *
 *   Fax, PriceCode, Others, SVatNo, AreaCode, CustomerType,
 *   EntryStatus, MYEntryDateTime  → skipped
 *
 * Run: npx tsx scripts/migrate-outlets.ts
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

// ── Migration config ───────────────────────────────────────────────────────
const DEFAULT_USER_ID = 3
const BATCH_SIZE = 500

// ── Enum maps ─────────────────────────────────────────────────────────────
// TODO: replace keys with the actual legacy integer values from CustomerMaster
// PostgreSQL values: Small=0, Medium=1, Large=2
const OUTLET_TYPE_MAP: Record<number, number> = {
  1: 0, // TODO: confirm legacy value for Small
  2: 1, // TODO: confirm legacy value for Medium
  3: 2, // TODO: confirm legacy value for Large
}

// PostgreSQL values: Wholesale=0, SMMT=1
const OUTLET_CATEGORY_MAP: Record<number, number> = {
  1: 0, // TODO: confirm legacy value for Wholesale
  2: 1, // TODO: confirm legacy value for SMMT
}

// PostgreSQL values: DealerPrice=0, OldPrice=1, MarketPrice=2
const BILLING_PRICE_MAP: Record<number, number> = {
  1: 0, // TODO: confirm legacy value for DealerPrice
  2: 1, // TODO: confirm legacy value for OldPrice
  3: 2, // TODO: confirm legacy value for MarketPrice
}

// ── Types ──────────────────────────────────────────────────────────────────
interface CustomerMasterRow {
  CustomerCode: number
  CustomerName: string
  Address: string | null
  Tel: string | null
  VatNo: string | null
  UserId: number | null
  EntryDate: Date | null
  ContactName: string | null
  Others: string | null
  Active: number
  Email: string | null
  NicNo: string | null
  Image: string | null
  CreditLimit: number | null
  RootCode: number | null
  CustomerLocation: string | null
  DOB: Date | null
  remarks: string | null
  ProvinceCode: number | null
  DistrictCode: number | null
  OutletTypeCode: number | null
  OutletCat: number | null
  BillingPriceType: number | null
  LastUpdateDate: Date | null
  LastUpdateUser: number | null
}

interface RouteRow {
  Id: number
  DivisionId: number
  TerritoryId: number
  AreaId: number
  RegionId: number
}

interface MappedRow {
  id: number
  name: string
  address: string
  tel: string
  email: string | null
  contactPerson: string | null
  nicNo: string
  vatNo: string | null
  creditLimit: number
  latitude: number
  longitude: number
  ownerDOB: Date | null
  remarks: string | null
  image: string | null
  outletType: number
  outletCategory: number
  billingPriceType: number | null
  provinceCode: number | null
  districtCode: number | null
  routeId: number
  divisionId: number
  territoryId: number
  areaId: number
  regionId: number
  isActive: boolean
  createdAt: Date
  updatedAt: Date
  createdBy: number | null
  updatedBy: number | null
}

// ── Helpers ────────────────────────────────────────────────────────────────

/** Parse 'lat,lng' string → [latitude, longitude]. Returns [0, 0] if unparseable. */
function parseLocation(location: string | null): [number, number] {
  if (!location) return [0, 0]
  const parts = location.split(',')
  if (parts.length < 2) return [0, 0]
  const lat = parseFloat(parts[0].trim())
  const lng = parseFloat(parts[1].trim())
  if (isNaN(lat) || isNaN(lng)) return [0, 0]
  return [lat, lng]
}

/** Build a multi-row INSERT with parameterized placeholders for a batch of rows.
 *  Each row has 29 columns → placeholders are $1..$29, $30..$58, etc.
 */
function buildBatchInsert(batch: MappedRow[]): { text: string; values: unknown[] } {
  const COLS_PER_ROW = 29
  const values: unknown[] = []
  const rowPlaceholders: string[] = []

  batch.forEach((row, i) => {
    const base = i * COLS_PER_ROW
    rowPlaceholders.push(
      `($${base + 1}, $${base + 2}, $${base + 3}, $${base + 4}, $${base + 5}, ` +
      `$${base + 6}, $${base + 7}, $${base + 8}, $${base + 9}, $${base + 10}, ` +
      `$${base + 11}, $${base + 12}, $${base + 13}, $${base + 14}, $${base + 15}, ` +
      `$${base + 16}, $${base + 17}, $${base + 18}, $${base + 19}, $${base + 20}, ` +
      `$${base + 21}, $${base + 22}, $${base + 23}, $${base + 24}, $${base + 25}, ` +
      `$${base + 26}, $${base + 27}, $${base + 28}, $${base + 29})`
    )
    values.push(
      row.id,             // $1  Id
      row.name,           // $2  Name
      row.address,        // $3  Address
      row.tel,            // $4  Tel
      row.email,          // $5  Email
      row.contactPerson,  // $6  ContactPerson
      row.nicNo,          // $7  NicNo
      row.vatNo,          // $8  VatNo
      row.creditLimit,    // $9  CreditLimit
      row.latitude,       // $10 Latitude
      row.longitude,      // $11 Longitude
      row.ownerDOB,       // $12 OwnerDOB
      row.remarks,        // $13 Remarks
      row.image,          // $14 Image
      row.outletType,     // $15 OutletType
      row.outletCategory, // $16 OutletCategory
      row.billingPriceType, // $17 BillingPriceType
      row.provinceCode,   // $18 ProvinceCode
      row.districtCode,   // $19 DistrictCode
      row.routeId,        // $20 RouteId
      row.divisionId,     // $21 DivisionId
      row.territoryId,    // $22 TerritoryId
      row.areaId,         // $23 AreaId
      row.regionId,       // $24 RegionId
      row.isActive,       // $25 IsActive
      row.createdAt,      // $26 CreatedAt
      row.updatedAt,      // $27 UpdatedAt
      row.createdBy,      // $28 CreatedBy
      row.updatedBy       // $29 UpdatedBy
    )
  })

  const text = `
    INSERT INTO "Outlets" (
      "Id", "Name", "Address", "Tel", "Email", "ContactPerson",
      "NicNo", "VatNo", "CreditLimit", "Latitude", "Longitude",
      "OwnerDOB", "Remarks", "Image", "OutletType", "OutletCategory",
      "BillingPriceType", "ProvinceCode", "DistrictCode",
      "RouteId", "DivisionId", "TerritoryId", "AreaId", "RegionId",
      "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
    )
    VALUES ${rowPlaceholders.join(', ')}
    ON CONFLICT ("Id") DO UPDATE SET
      "Name"            = EXCLUDED."Name",
      "Address"         = EXCLUDED."Address",
      "Tel"             = EXCLUDED."Tel",
      "Email"           = EXCLUDED."Email",
      "ContactPerson"   = EXCLUDED."ContactPerson",
      "NicNo"           = EXCLUDED."NicNo",
      "VatNo"           = EXCLUDED."VatNo",
      "CreditLimit"     = EXCLUDED."CreditLimit",
      "Latitude"        = EXCLUDED."Latitude",
      "Longitude"       = EXCLUDED."Longitude",
      "OwnerDOB"        = EXCLUDED."OwnerDOB",
      "Remarks"         = EXCLUDED."Remarks",
      "Image"           = EXCLUDED."Image",
      "OutletType"      = EXCLUDED."OutletType",
      "OutletCategory"  = EXCLUDED."OutletCategory",
      "BillingPriceType"= EXCLUDED."BillingPriceType",
      "ProvinceCode"    = EXCLUDED."ProvinceCode",
      "DistrictCode"    = EXCLUDED."DistrictCode",
      "RouteId"         = EXCLUDED."RouteId",
      "DivisionId"      = EXCLUDED."DivisionId",
      "TerritoryId"     = EXCLUDED."TerritoryId",
      "AreaId"          = EXCLUDED."AreaId",
      "RegionId"        = EXCLUDED."RegionId",
      "IsActive"        = EXCLUDED."IsActive",
      "UpdatedAt"       = EXCLUDED."UpdatedAt",
      "UpdatedBy"       = EXCLUDED."UpdatedBy"
  `
  return { text, values }
}

// ── Main ───────────────────────────────────────────────────────────────────
async function migrate() {
  console.log('Connecting to SQL Server...')
  const sqlPool = await sql.connect(sqlConfig)
  console.log('Connected to SQL Server ✓')

  // 1. Read all rows from SQL Server
  const result = await sqlPool.request().query<CustomerMasterRow>(`
    SELECT
      CustomerCode, CustomerName, Address, Tel, VatNo, UserId, EntryDate,
      ContactName, Others, Active, Email, NicNo, Image, CreditLimit,
      RootCode, CustomerLocation, DOB, remarks, ProvinceCode, DistrictCode,
      OutletTypeCode, OutletCat, BillingPriceType, LastUpdateDate, LastUpdateUser
    FROM [SeefaUswattaSFABiscut].[dbo].[CustomerMaster]
    ORDER BY CustomerCode
  `)
  console.log(`Found ${result.recordset.length} rows in CustomerMaster`)

  // 2. Connect to PostgreSQL and load lookups
  console.log('Connecting to PostgreSQL...')
  const pgClient = await pg.connect()
  console.log('Connected to PostgreSQL ✓\n')

  // Pre-load all Routes for in-memory lookup (RootCode → RouteRow)
  const routesResult = await pgClient.query<RouteRow>(`
    SELECT "Id", "DivisionId", "TerritoryId", "AreaId", "RegionId"
    FROM "Routes"
  `)
  const routeMap = new Map<number, RouteRow>()
  for (const r of routesResult.rows) routeMap.set(r.Id, r)
  console.log(`Loaded ${routeMap.size} routes from PostgreSQL\n`)

  // 3. Transform — filter invalid rows, map columns
  const validRows: MappedRow[] = []
  let skippedInvalid = 0
  let skippedNoRoute = 0

  for (const row of result.recordset) {
    const name = row.CustomerName?.trim()

    if (!row.CustomerCode || !name) {
      console.warn(`  ⚠ Skipping — null/invalid fields (CustomerCode=${row.CustomerCode}, Name=${name})`)
      skippedInvalid++
      continue
    }

    // NicNo may be null or duplicated in legacy data — fallback to CustomerCode-based value
    const nicNo = row.NicNo?.trim() || `LEGACY-${row.CustomerCode}`

    // Skip if RootCode not found in Routes
    const route = row.RootCode != null ? routeMap.get(row.RootCode) : undefined
    if (!route) {
      console.warn(`  ⚠ Skipping — RootCode=${row.RootCode} not found in Routes (CustomerCode=${row.CustomerCode}, Name=${name})`)
      skippedNoRoute++
      continue
    }

    const [latitude, longitude] = parseLocation(row.CustomerLocation)

    const remarks = [row.remarks?.trim(), row.Others?.trim()]
      .filter(Boolean)
      .join(' | ') || null

    validRows.push({
      id: row.CustomerCode,
      name,
      address: row.Address?.trim() ?? '',
      tel: row.Tel?.trim() ?? '',
      email: row.Email?.trim() || null,
      contactPerson: row.ContactName?.trim() || null,
      nicNo,
      vatNo: row.VatNo?.trim() || null,
      creditLimit: row.CreditLimit ?? 0,
      latitude,
      longitude,
      ownerDOB: row.DOB ? new Date(row.DOB) : null,
      remarks,
      image: row.Image?.trim() || null,
      outletType: OUTLET_TYPE_MAP[row.OutletTypeCode ?? 0] ?? 0,
      outletCategory: OUTLET_CATEGORY_MAP[row.OutletCat ?? 0] ?? 0,
      billingPriceType: row.BillingPriceType != null
        ? (BILLING_PRICE_MAP[row.BillingPriceType] ?? null)
        : null,
      provinceCode: row.ProvinceCode ?? null,
      districtCode: row.DistrictCode ?? null,
      routeId: route.Id,
      divisionId: route.DivisionId,
      territoryId: route.TerritoryId,
      areaId: route.AreaId,
      regionId: route.RegionId,
      isActive: row.Active === 1,
      createdAt: row.EntryDate ? new Date(row.EntryDate) : new Date(),
      updatedAt: row.LastUpdateDate ? new Date(row.LastUpdateDate) : new Date(),
      createdBy: DEFAULT_USER_ID,
      updatedBy: DEFAULT_USER_ID,
    })
  }

  console.log(`Valid rows to insert/update: ${validRows.length}`)
  console.log(`Skipped — invalid fields:    ${skippedInvalid}`)
  console.log(`Skipped — route not found:   ${skippedNoRoute}`)
  console.log(`Note: duplicate/null NicNos replaced with LEGACY-<CustomerCode>\n`)

  // 4. Insert into PostgreSQL in batches
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
      console.log(`  Batch ${batchNum}/${totalBatches} → inserted/updated ${inserted}/${validRows.length} rows`)
    }

    // Reset sequence so future app inserts don't collide with migrated IDs
    await pgClient.query(`
      SELECT setval(
        pg_get_serial_sequence('"Outlets"', 'Id'),
        (SELECT MAX("Id") FROM "Outlets")
      )
    `)

    await pgClient.query('COMMIT')
    console.log(`\nDone! Inserted/updated: ${inserted}`)
    console.log(`Skipped (invalid): ${skippedInvalid} | (no route): ${skippedNoRoute}`)
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
