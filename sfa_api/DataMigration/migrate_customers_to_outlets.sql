-- ============================================================
-- DATA MIGRATION: MSSQL CustomerMaster → PostgreSQL Outlets
-- ============================================================
-- Prerequisites:
--   1. Export CustomerMaster from MSSQL (CSV / BCP / SSMS)
--   2. COPY the export into the staging table below
--   3. Confirm enum value mappings marked with TODO
--   4. Run Step 1, then Step 2, then Step 3 (verify)
--
-- Rows where RootCode does not exist in "Routes" are SKIPPED.
-- Rows where NicNo already exists in "Outlets" are SKIPPED.
--
-- Enum integer values (EF Core default storage):
--   OutletType:       Small=0  | Medium=1  | Large=2
--   OutletCategory:   Wholesale=0 | SMMT=1
--   BillingPriceType: DealerPrice=0 | OldPrice=1 | MarketPrice=2
-- ============================================================


-- ============================================================
-- STEP 1: Create staging table and load MSSQL data into it
-- ============================================================

CREATE TABLE IF NOT EXISTS staging_customer_master (
    "CustomerCode"     VARCHAR(50),
    "CustomerName"     VARCHAR(255),
    "Address"          TEXT,
    "Tel"              VARCHAR(50),
    "VatNo"            VARCHAR(100),
    "UserId"           INT,
    "EntryDate"        TIMESTAMP,
    "ContactName"      VARCHAR(255),
    "Fax"              VARCHAR(50),
    "PriceCode"        INT,
    "Others"           TEXT,
    "Active"           SMALLINT,        -- 1 = active, 0 = inactive
    "AreaCode"         INT,
    "SVatNo"           VARCHAR(100),
    "Email"            VARCHAR(255),
    "NicNo"            VARCHAR(50),
    "Image"            VARCHAR(500),
    "CreditLimit"      NUMERIC(18,2),
    "RootCode"         INT,             -- maps to Routes.Id in PostgreSQL
    "CustomerType"     INT,
    "CustomerLocation" VARCHAR(500),    -- format: 'latitude,longitude'
    "DOB"              TIMESTAMP,
    "remarks"          TEXT,
    "ProvinceCode"     INT,
    "DistrictCode"     INT,
    "OutletTypeCode"   INT,
    "OutletCat"        INT,
    "BillingPriceType" INT,
    "EntryStatus"      INT,
    "LastUpdateDate"   TIMESTAMP,
    "LastUpdateUser"   INT,
    "MYEntryDateTime"  TIMESTAMP
);

-- After creating the staging table, load your MSSQL export:
-- COPY staging_customer_master FROM '/path/to/export.csv' WITH (FORMAT csv, HEADER true);


-- ============================================================
-- STEP 2: Insert into Outlets
--         Rows skipped if RootCode not found in Routes
--         Rows skipped if NicNo already exists in Outlets
-- ============================================================

INSERT INTO "Outlets" (
    "Name",
    "Address",
    "Tel",
    "Email",
    "ContactPerson",
    "NicNo",
    "VatNo",
    "CreditLimit",
    "Latitude",
    "Longitude",
    "OwnerDOB",
    "Remarks",
    "Image",
    "OutletType",
    "OutletCategory",
    "BillingPriceType",
    "ProvinceCode",
    "DistrictCode",
    "RouteId",
    "DivisionId",
    "TerritoryId",
    "AreaId",
    "RegionId",
    "IsActive",
    "CreatedAt",
    "UpdatedAt",
    "CreatedBy",
    "UpdatedBy"
)
SELECT
    -- ── Identity ─────────────────────────────────────────────────────────────
    COALESCE(NULLIF(TRIM(s."CustomerName"), ''), 'Unknown')             AS "Name",
    COALESCE(NULLIF(TRIM(s."Address"), ''), '')                         AS "Address",
    COALESCE(NULLIF(TRIM(s."Tel"), ''), '')                             AS "Tel",
    NULLIF(TRIM(s."Email"), '')                                         AS "Email",
    NULLIF(TRIM(s."ContactName"), '')                                   AS "ContactPerson",
    COALESCE(NULLIF(TRIM(s."NicNo"), ''), 'UNKNOWN-' || s."CustomerCode") AS "NicNo",
    NULLIF(TRIM(s."VatNo"), '')                                         AS "VatNo",
    COALESCE(s."CreditLimit", 0)                                        AS "CreditLimit",

    -- ── Location ─────────────────────────────────────────────────────────────
    -- CustomerLocation format assumed: 'latitude,longitude'
    -- If format is different, adjust SPLIT_PART accordingly
    CASE
        WHEN s."CustomerLocation" IS NOT NULL AND s."CustomerLocation" LIKE '%,%'
        THEN CAST(TRIM(SPLIT_PART(s."CustomerLocation", ',', 1)) AS DOUBLE PRECISION)
        ELSE 0.0
    END                                                                 AS "Latitude",
    CASE
        WHEN s."CustomerLocation" IS NOT NULL AND s."CustomerLocation" LIKE '%,%'
        THEN CAST(TRIM(SPLIT_PART(s."CustomerLocation", ',', 2)) AS DOUBLE PRECISION)
        ELSE 0.0
    END                                                                 AS "Longitude",

    -- ── Dates ────────────────────────────────────────────────────────────────
    CASE
        WHEN s."DOB" IS NOT NULL THEN (s."DOB"::TIMESTAMPTZ AT TIME ZONE 'UTC')
        ELSE NULL
    END                                                                 AS "OwnerDOB",

    -- ── Remarks: merge remarks + Others ──────────────────────────────────────
    CASE
        WHEN s."remarks" IS NOT NULL AND s."Others" IS NOT NULL
            THEN s."remarks" || ' | ' || s."Others"
        ELSE COALESCE(s."remarks", s."Others")
    END                                                                 AS "Remarks",

    NULLIF(TRIM(s."Image"), '')                                         AS "Image",

    -- ── OutletType enum: Small=0, Medium=1, Large=2 ──────────────────────────
    -- TODO: replace WHEN values with actual legacy OutletTypeCode integers
    CASE s."OutletTypeCode"
        WHEN 1 THEN 0   -- TODO: confirm legacy value for Small
        WHEN 2 THEN 1   -- TODO: confirm legacy value for Medium
        WHEN 3 THEN 2   -- TODO: confirm legacy value for Large
        ELSE 0          -- default: Small
    END                                                                 AS "OutletType",

    -- ── OutletCategory enum: Wholesale=0, SMMT=1 ─────────────────────────────
    -- Uses OutletCat; falls back to CustomerType if OutletCat is null
    -- TODO: replace WHEN values with actual legacy integers
    CASE COALESCE(s."OutletCat", s."CustomerType")
        WHEN 1 THEN 0   -- TODO: confirm legacy value for Wholesale
        WHEN 2 THEN 1   -- TODO: confirm legacy value for SMMT
        ELSE 0          -- default: Wholesale
    END                                                                 AS "OutletCategory",

    -- ── BillingPriceType enum: DealerPrice=0, OldPrice=1, MarketPrice=2 ──────
    -- TODO: replace WHEN values with actual legacy integers
    CASE s."BillingPriceType"
        WHEN 1 THEN 0   -- TODO: confirm legacy value for DealerPrice
        WHEN 2 THEN 1   -- TODO: confirm legacy value for OldPrice
        WHEN 3 THEN 2   -- TODO: confirm legacy value for MarketPrice
        ELSE NULL
    END                                                                 AS "BillingPriceType",

    -- ── Geography ────────────────────────────────────────────────────────────
    s."ProvinceCode"                                                    AS "ProvinceCode",
    s."DistrictCode"                                                    AS "DistrictCode",

    -- ── Route hierarchy from Routes table (via RootCode = Routes.Id) ─────────
    r."Id"                                                              AS "RouteId",
    r."DivisionId"                                                      AS "DivisionId",
    r."TerritoryId"                                                     AS "TerritoryId",
    r."AreaId"                                                          AS "AreaId",
    r."RegionId"                                                        AS "RegionId",

    -- ── Status & Audit ───────────────────────────────────────────────────────
    (s."Active" = 1)                                                    AS "IsActive",
    COALESCE(s."EntryDate", NOW())::TIMESTAMPTZ AT TIME ZONE 'UTC'      AS "CreatedAt",
    COALESCE(s."LastUpdateDate", NOW())::TIMESTAMPTZ AT TIME ZONE 'UTC' AS "UpdatedAt",
    s."UserId"                                                          AS "CreatedBy",
    s."LastUpdateUser"                                                  AS "UpdatedBy"

FROM staging_customer_master s

-- INNER JOIN: rows with no matching Route in PostgreSQL are automatically skipped
INNER JOIN "Routes" r ON r."Id" = s."RootCode"

-- Skip rows where NicNo already exists in Outlets
WHERE NOT EXISTS (
    SELECT 1
    FROM "Outlets" o
    WHERE o."NicNo" = s."NicNo"
);


-- ============================================================
-- STEP 3: Verify migration results
-- ============================================================

SELECT
    (SELECT COUNT(*) FROM staging_customer_master)                      AS total_staged,
    (SELECT COUNT(*) FROM "Outlets")                                    AS total_in_outlets,
    (SELECT COUNT(*)
     FROM staging_customer_master s
     WHERE NOT EXISTS (
         SELECT 1 FROM "Routes" r WHERE r."Id" = s."RootCode"
     ))                                                                 AS skipped_route_not_found,
    (SELECT COUNT(*)
     FROM staging_customer_master s
     WHERE EXISTS (
         SELECT 1 FROM "Outlets" o WHERE o."NicNo" = s."NicNo"
     ))                                                                 AS skipped_duplicate_nic;


-- ============================================================
-- STEP 4 (optional): Inspect skipped rows
-- ============================================================

-- Rows skipped because RootCode not found in Routes:
SELECT s.*
FROM staging_customer_master s
WHERE NOT EXISTS (
    SELECT 1 FROM "Routes" r WHERE r."Id" = s."RootCode"
);

-- Rows skipped because NicNo already exists in Outlets:
SELECT s.*
FROM staging_customer_master s
WHERE EXISTS (
    SELECT 1 FROM "Outlets" o WHERE o."NicNo" = s."NicNo"
);


-- ============================================================
-- STEP 5 (cleanup): Drop staging table after migration is confirmed
-- ============================================================

-- DROP TABLE staging_customer_master;
