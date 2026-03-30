# Reference: High-Growth Indexing & Partitioning

Load this reference when the entity will exceed 10M rows — typically orders, visits, check-ins, route schedules, audit logs.

---

## Identifying High-Growth Entities

| Entity Type | Growth Rate | Trigger |
|-------------|------------|---------|
| Sales Orders | ~1000/day per distributor | Order creation from field reps |
| Visit Reports | ~500/day per territory | Daily field activity |
| Check-ins/Check-outs | ~1000/day per territory | GPS-tracked rep movements |
| Route Schedules | ~200/day per territory | Auto-generated daily plans |
| Audit Logs | ~10000/day | Every write operation logged |
| Notifications | ~5000/day | System and user notifications |

**Rule of thumb:** If the entity accumulates >100 rows/day across all tenants, treat as high-growth.

---

## Composite Partial Indexes (Required for All Entities)

Standard partial index — every entity gets this:

```csharp
e.HasIndex(x => new { x.IsActive, x.CreatedAt })
 .IsDescending(false, true)
 .HasFilter("\"IsActive\" = true")
 .HasDatabaseName("idx_{entities}_active_created");
```

### Additional Indexes for High-Growth Entities

```csharp
// For queries: "my orders this month" — filtered by owner + date range
e.HasIndex(x => new { x.CreatedByUserId, x.CreatedAt })
 .IsDescending(false, true)
 .HasFilter("\"IsActive\" = true")
 .HasDatabaseName("idx_{entities}_user_created");

// For queries: "orders for this distributor" — filtered by FK + date
e.HasIndex(x => new { x.DistributorId, x.CreatedAt })
 .IsDescending(false, true)
 .HasFilter("\"IsActive\" = true")
 .HasDatabaseName("idx_{entities}_distributor_created");

// For queries: "pending approvals" — status-based
e.HasIndex(x => new { x.Status, x.CreatedAt })
 .IsDescending(false, true)
 .HasFilter("\"IsActive\" = true")
 .HasDatabaseName("idx_{entities}_status_created");

// For text search: GIN trigram index (raw SQL migration)
// Add this as a separate migration step:
// CREATE INDEX idx_{entities}_name_trgm ON "{Entities}"
//   USING gin ("Name" gin_trgm_ops) WHERE "IsActive" = true;
```

---

## Table Partitioning (PostgreSQL)

EF Core doesn't natively support partitioning. Add this as a migration TODO and execute manually in production.

### Range Partitioning by CreatedAt

```sql
-- Step 1: Rename original table
ALTER TABLE "{Entities}" RENAME TO "{Entities}_main";

-- Step 2: Create partitioned table with same schema
CREATE TABLE "{Entities}" (LIKE "{Entities}_main" INCLUDING ALL)
    PARTITION BY RANGE ("CreatedAt");

-- Step 3: Create partitions (one per year or quarter)
CREATE TABLE "{Entities}_2025" PARTITION OF "{Entities}"
    FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');
CREATE TABLE "{Entities}_2026" PARTITION OF "{Entities}"
    FOR VALUES FROM ('2026-01-01') TO ('2027-01-01');
CREATE TABLE "{Entities}_2027" PARTITION OF "{Entities}"
    FOR VALUES FROM ('2027-01-01') TO ('2028-01-01');

-- Step 4: Migrate data
INSERT INTO "{Entities}" SELECT * FROM "{Entities}_main";

-- Step 5: Drop old table
DROP TABLE "{Entities}_main";

-- Step 6: Create a DEFAULT partition for future-proofing
CREATE TABLE "{Entities}_default" PARTITION OF "{Entities}" DEFAULT;
```

### Monthly Partitioning (for very high volume)

```sql
-- For entities with >10k rows/day
CREATE TABLE "{Entities}_2025_01" PARTITION OF "{Entities}"
    FOR VALUES FROM ('2025-01-01') TO ('2025-02-01');
CREATE TABLE "{Entities}_2025_02" PARTITION OF "{Entities}"
    FOR VALUES FROM ('2025-02-01') TO ('2025-03-01');
-- ... etc
```

### Partition Maintenance Automation

```sql
-- Run monthly via pg_cron or scheduled job
DO $$
DECLARE
    next_month DATE := date_trunc('month', now()) + interval '2 months';
    partition_name TEXT := '{Entities}_' || to_char(next_month, 'YYYY_MM');
    start_date TEXT := to_char(next_month, 'YYYY-MM-DD');
    end_date TEXT := to_char(next_month + interval '1 month', 'YYYY-MM-DD');
BEGIN
    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS %I PARTITION OF "{Entities}" FOR VALUES FROM (%L) TO (%L)',
        partition_name, start_date, end_date
    );
END $$;
```

---

## DbContext Configuration for High-Growth

```csharp
modelBuilder.Entity<{FeatureName}>(e =>
{
    e.HasKey(x => x.Id);
    e.Property(x => x.Id).UseIdentityColumn();

    // Primary query index
    e.HasIndex(x => new { x.IsActive, x.CreatedAt })
     .IsDescending(false, true)
     .HasFilter("\"IsActive\" = true")
     .HasDatabaseName("idx_{entities}_active_created");

    // FK-based query indexes
    e.HasIndex(x => new { x.CreatedByUserId, x.CreatedAt })
     .IsDescending(false, true)
     .HasFilter("\"IsActive\" = true")
     .HasDatabaseName("idx_{entities}_user_created");

    // Status-based query index
    e.HasIndex(x => new { x.Status, x.CreatedAt })
     .IsDescending(false, true)
     .HasFilter("\"IsActive\" = true")
     .HasDatabaseName("idx_{entities}_status_created");
});

// TODO (PRODUCTION): RANGE-partition "{Entities}" by CreatedAt.
// See references/high-growth-indexing.md for exact SQL commands.
```

---

## Query Patterns for Partitioned Tables

EF Core queries work transparently — PostgreSQL routes to the correct partition. But always include the partition key in WHERE clauses for partition pruning:

```csharp
// GOOD — partition pruning activates (only scans relevant partitions)
var orders = await _context.SalesOrders
    .Where(o => o.IsActive && o.CreatedAt >= startDate && o.CreatedAt < endDate)
    .AsNoTracking()
    .Skip(skip).Take(take)
    .ToListAsync(ct);

// BAD — no partition pruning (scans all partitions)
var orders = await _context.SalesOrders
    .Where(o => o.IsActive)
    .AsNoTracking()
    .ToListAsync(ct);
```

---

## Archive Strategy

For entities older than the retention period:

```sql
-- Move old data to archive table (not partitioned, cold storage)
INSERT INTO "{Entities}_archive"
SELECT * FROM "{Entities}"
WHERE "CreatedAt" < now() - interval '2 years';

-- Remove from hot table
DELETE FROM "{Entities}"
WHERE "CreatedAt" < now() - interval '2 years';

-- Or simply detach the old partition
ALTER TABLE "{Entities}" DETACH PARTITION "{Entities}_2023";
```
