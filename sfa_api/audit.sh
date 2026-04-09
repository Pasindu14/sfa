#!/bin/bash

# ─────────────────────────────────────────────
# SFA API — Production Readiness Audit Script
# Run from: E:\Github\sfa\sfa_api
# Usage:    bash audit.sh              → full audit
#           bash audit.sh Areas        → feature-scoped audit
# Output:   audit-report.txt
# ─────────────────────────────────────────────

SRC="./sfa_api"
REPORT="./audit-report.txt"
PASS=0
FAIL=0

# ─────────────────────────────────────────────
# CONFIGURE: Tables that grow unboundedly
# These require cursor-based pagination
# Add new heavy tables here as schema grows
# ─────────────────────────────────────────────
HEAVY_TABLES="order|visit|audit|gps|log|transaction|sync|invoice|payment|attendance|checkin|activity|history|stockmovement|stock.movement"

# ─────────────────────────────────────────────
# Feature filter — scoped or full
# ─────────────────────────────────────────────
FEATURE="${1:-}"
if [ -n "$FEATURE" ]; then
  FEATURES_DIR="$SRC/Features/$FEATURE"
  if [ ! -d "$FEATURES_DIR" ]; then
    echo "ERROR: Feature directory not found: $FEATURES_DIR"
    exit 1
  fi
  echo "Auditing feature: $FEATURE"
else
  FEATURES_DIR="$SRC/Features"
  echo "Auditing all features"
fi

> "$REPORT"

log_pass() { echo "  PASS — $1" >> "$REPORT"; ((PASS++)); }
log_fail() { echo "  FAIL — $1" >> "$REPORT"; ((FAIL++)); }
section()  { echo "" >> "$REPORT"; echo "## $1" >> "$REPORT"; }

SCOPE="${FEATURE:-ALL FEATURES}"
echo "SFA API Audit Report — $(date) [Scope: $SCOPE]" >> "$REPORT"
echo "============================================" >> "$REPORT"

# ─────────────────────────────────────────────
section "1. Error Handling"
# ─────────────────────────────────────────────

FILES=$(grep -rl "try" "$FEATURES_DIR" --include="*Endpoint*.cs" --include="*Controller*.cs" 2>/dev/null)
if [ -n "$FILES" ]; then
  for f in $FILES; do log_fail "try-catch in endpoint: $f"; done
else log_pass "No try-catch in endpoints"; fi

MATCHES=$(grep -rn "throw new Exception(" "$FEATURES_DIR" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "Untyped exception: $line"; done <<< "$MATCHES"
else log_pass "No untyped exceptions"; fi

VALIDATORS=$(find "$FEATURES_DIR" -name "*Validator*.cs" 2>/dev/null | wc -l)
REQUESTS=$(find "$FEATURES_DIR" -name "*Request*.cs" 2>/dev/null | wc -l)
if [ "$REQUESTS" -gt 0 ] && [ "$VALIDATORS" -lt "$REQUESTS" ]; then
  log_fail "Possible missing validators: $VALIDATORS validators vs $REQUESTS request files"
else log_pass "Validator count matches request count ($VALIDATORS/$REQUESTS)"; fi

# ─────────────────────────────────────────────
section "2. Stock Race Conditions"
# ─────────────────────────────────────────────

grep -rn "FOR UPDATE SKIP LOCKED" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "SELECT FOR UPDATE SKIP LOCKED found" \
  || log_fail "SELECT FOR UPDATE SKIP LOCKED missing — stock updates unprotected"

grep -rn "pg_advisory_xact_lock\|pg_advisory_lock" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Advisory lock found" \
  || log_fail "pg_advisory_xact_lock not found — business-level locking missing"

MATCHES=$(grep -rn "stock.*-=\|Stock.*-=\|\bstock\b.*update\|\bStock\b.*Update" "$FEATURES_DIR" --include="*.cs" 2>/dev/null \
  | grep -v "ExecuteSql\|FromSql\|StoredProc\|RawSql\|test\|Test")
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "Stock deduction in app layer (must be atomic SQL): $line"; done <<< "$MATCHES"
else log_pass "No app-layer stock deduction detected"; fi

grep -rn "NegativeStock\|stock.*< 0\|quantity.*< 0\|Stock.*< 0" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Negative stock guard found" \
  || log_fail "No negative stock guard — alert missing if stock goes below zero"

grep -rn "Redlock\|RedLock\|SETNX\|SetIfNotExists" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Redis mutex (Redlock/SETNX) found" \
  || log_fail "No Redis mutex — consider Redlock before DB for high-contention stock"

# ─────────────────────────────────────────────
section "3. Concurrency"
# ─────────────────────────────────────────────

grep -rn "RowVersion\|xmin\|ConcurrencyCheck\|\[Timestamp\]" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Optimistic concurrency token found" \
  || log_fail "No optimistic concurrency token — add RowVersion/xmin for non-stock entities"

grep -rn "IdempotencyKey\|idempotency.key\|idempotent" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Idempotency key found" \
  || log_fail "No idempotency key — duplicate order submissions possible on retry"

# ─────────────────────────────────────────────
section "4. Caching"
# ─────────────────────────────────────────────

grep -rn "IDistributedCache\|IConnectionMultiplexer\|StackExchange.Redis" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Redis cache found" \
  || log_fail "Redis not found — master data must be cached (products, routes, outlets, price lists)"

grep -rn "AddStackExchangeRedisCache\|AddRedis" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Redis registered in Program.cs" \
  || log_fail "Redis not registered in Program.cs"

# ─────────────────────────────────────────────
section "5. Performance"
# ─────────────────────────────────────────────

REPO_FILES=$(grep -rln "\.Where\|\.FirstOrDefault\|\.ToList\|\.ToListAsync" "$FEATURES_DIR" --include="*Repository*.cs" 2>/dev/null)
for f in $REPO_FILES; do
  if ! grep -q "AsNoTracking" "$f"; then
    log_fail "Missing AsNoTracking in: $f"
  else log_pass "AsNoTracking present: $f"; fi
done

MATCHES=$(grep -rn "\.ToListAsync" "$FEATURES_DIR" --include="*Repository*.cs" 2>/dev/null \
  | grep -v "Take\|Skip\|Paginate\|cursor\|Count\|Any")
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "Possible unbounded query: $line"; done <<< "$MATCHES"
else log_pass "No unbounded ToListAsync found"; fi

MATCHES=$(grep -rn "foreach\|\.ForEach" "$FEATURES_DIR" --include="*.cs" -A3 2>/dev/null \
  | grep -E "GetAsync|FindAsync|ToListAsync|FirstOrDefaultAsync")
if [ -n "$MATCHES" ]; then
  log_fail "Possible N+1 — DB call inside loop detected"
else log_pass "No obvious N+1 pattern detected"; fi

MATCHES=$(grep -rn 'ILogger.*\$"' "$SRC" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "String interpolation in log: $line"; done <<< "$MATCHES"
else log_pass "No string interpolation in logs"; fi

# ─────────────────────────────────────────────
section "6. Pagination"
# ─────────────────────────────────────────────

SKIP_TAKE_FILES=$(grep -rln "\.Skip(.*).\.Take(" "$FEATURES_DIR" --include="*.cs" 2>/dev/null)
HEAVY_OFFSET_FOUND=0
for f in $SKIP_TAKE_FILES; do
  if echo "$f" | grep -iE "$HEAVY_TABLES" > /dev/null 2>&1; then
    log_fail "Offset pagination on heavy table file (use cursor): $f"
    HEAVY_OFFSET_FOUND=1
  elif grep -iE "$HEAVY_TABLES" "$f" > /dev/null 2>&1; then
    log_fail "Offset pagination referencing heavy table (use cursor): $f"
    HEAVY_OFFSET_FOUND=1
  fi
done
[ "$HEAVY_OFFSET_FOUND" -eq 0 ] && log_pass "No offset pagination on heavy tables"

grep -rn "WHERE.*id >.*@last\|cursor\|CursorId\|AfterCursor" --include="*.cs" "$FEATURES_DIR" > /dev/null 2>&1 \
  && log_pass "Cursor-based pagination found" \
  || log_fail "No cursor pagination — heavy tables must use cursor not offset"

# ─────────────────────────────────────────────
section "7. Connection Pooling"
# ─────────────────────────────────────────────

grep -rn "Pooling=true\|Maximum Pool Size\|MinPoolSize\|PgBouncer\|Npgsql.*Pool" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Connection pooling configured" \
  || log_fail "No explicit pooling config — confirm Npgsql default pooling or PgBouncer in place"

MATCHES=$(grep -rn "AddDbContext\b" "$SRC" --include="*.cs" 2>/dev/null | grep -v "AddDbContextPool")
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "AddDbContext used instead of AddDbContextPool: $line"; done <<< "$MATCHES"
else log_pass "AddDbContextPool used"; fi

# ─────────────────────────────────────────────
section "8. Database Indexes"
# ─────────────────────────────────────────────

grep -rn "HasIndex\|CreateIndex" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Indexes found" \
  || log_fail "No indexes found — Date and Status columns must be indexed"

grep -rn "HasIndex.*[Dd]ate\|CreateIndex.*[Dd]ate\|ix_.*date\|idx_.*date" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Date column index found" \
  || log_fail "No Date index — required for order/visit queries"

grep -rn "HasIndex.*[Ss]tatus\|CreateIndex.*[Ss]tatus\|ix_.*status\|idx_.*status" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Status column index found" \
  || log_fail "No Status index — add index on Status for filtered queries"

# ─────────────────────────────────────────────
section "9. Reporting"
# ─────────────────────────────────────────────

grep -rn "ReadReplica\|read.replica\|secondary.*connection\|replica.*connection\|ReplicaConnection" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Read replica connection found" \
  || log_fail "No read replica — reports must never hit primary DB"

grep -rn "statement_timeout\|StatementTimeout\|CommandTimeout" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Statement/command timeout configured" \
  || log_fail "No statement timeout — runaway report queries will kill the DB"

grep -rn "MATERIALIZED VIEW\|materialized.view\|MaterializedView" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Materialized views referenced" \
  || log_fail "No materialized views — heavy reports must use pre-aggregated views"

grep -rn "Hangfire\|BackgroundJob\|IBackgroundJobClient" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Hangfire background jobs found" \
  || log_fail "No Hangfire — heavy reports must be async (queue → process → Azure Blob → notify)"

MATCHES=$(grep -rn "SalesOrder.*Join\|Join.*SalesOrder\|OrderLine.*Join\|Join.*OrderLine" "$FEATURES_DIR" --include="*.cs" 2>/dev/null \
  | grep -i "stock\|Stock")
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "Real-time join on SalesOrders+Stock — use materialized view: $line"; done <<< "$MATCHES"
else log_pass "No real-time SalesOrders+Stock join detected"; fi

# ─────────────────────────────────────────────
section "10. Rate Limiting"
# ─────────────────────────────────────────────

grep -rn "RateLimiter\|AddRateLimiter\|AspNetCoreRateLimit\|SlidingWindow" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Rate limiting found" \
  || log_fail "No rate limiting — 500 reps can hammer API without limit"

grep -rn "userId\|UserId" --include="*.cs" "$SRC" \
  | grep -i "rate\|limit\|window\|policy" > /dev/null 2>&1 \
  && log_pass "Per-user rate limiting policy found" \
  || log_fail "No per-user rate limiting — must be per-rep (userId) not global only"

# ─────────────────────────────────────────────
section "11. Resiliency"
# ─────────────────────────────────────────────

grep -rn "Polly\|AddPolicyHandler\|RetryPolicy\|CircuitBreaker\|TimeoutPolicy" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Polly resiliency policies found" \
  || log_fail "No Polly — add timeout + retry policies on outbound calls"

MATCHES=$(grep -rn "async Task\|async IActionResult\|async IResult" "$FEATURES_DIR" --include="*.cs" 2>/dev/null \
  | grep -v "CancellationToken")
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "Missing CancellationToken: $line"; done <<< "$MATCHES"
else log_pass "All async methods have CancellationToken"; fi

CT_MISSING=$(grep -rn "ToListAsync()\|FirstOrDefaultAsync()\|SingleOrDefaultAsync()\|SaveChangesAsync()" \
  "$FEATURES_DIR" --include="*.cs" 2>/dev/null \
  | grep -v "cancellationToken\|ct)\|CancellationToken" | wc -l)
[ "$CT_MISSING" -eq 0 ] \
  && log_pass "CancellationToken propagated into EF calls" \
  || log_fail "$CT_MISSING EF async calls missing CancellationToken propagation"

# ─────────────────────────────────────────────
section "12. Response Compression"
# ─────────────────────────────────────────────

grep -rn "UseResponseCompression\|AddResponseCompression\|BrotliCompressionProvider" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Response compression enabled" \
  || log_fail "No response compression — critical for mobile reps on slow connections"

# ─────────────────────────────────────────────
section "13. Mobile Sync"
# ─────────────────────────────────────────────

grep -rn "IdempotencyKey\|idempotency.key" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Idempotency key on sync endpoint found" \
  || log_fail "No idempotency key on sync — duplicate orders on retry not protected"

grep -rn "SyncStatus\|sync.*accepted\|sync.*rejected\|sync.*partial" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Per-order sync status response found" \
  || log_fail "No per-order sync status — mobile needs accepted/rejected/partial per order"

grep -rn "server.*wins\|conflict.*server\|timestamp.*conflict\|UpdatedAt.*reject" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Server-wins conflict strategy found" \
  || log_fail "No explicit conflict resolution — server must always win on stock/price"

# ─────────────────────────────────────────────
section "14. Security"
# ─────────────────────────────────────────────

FILES=$(find "$FEATURES_DIR" -name "*Endpoint*.cs" -o -name "*Controller*.cs" 2>/dev/null)
for f in $FILES; do
  if ! grep -q "\[Authorize" "$f"; then
    log_fail "Missing [Authorize]: $f"
  else log_pass "[Authorize] present: $f"; fi
done

MATCHES=$(grep -rn "CompanyId\|company_id\|companyId" "$SRC" --include="*.cs" 2>/dev/null \
  | grep -v "test\|Test\|\.git")
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "CompanyId found — single-company system, remove: $line"; done <<< "$MATCHES"
else log_pass "No CompanyId found — correct for single-company system"; fi

MATCHES=$(grep -rn "password\s*=\s*['\"].\|secret\s*=\s*['\"].\|apikey\s*=\s*['\"]." "$SRC" --include="*.cs" -i 2>/dev/null)
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "Hardcoded secret: $line"; done <<< "$MATCHES"
else log_pass "No hardcoded secrets found"; fi

grep -rn "RefreshToken\|refresh_token" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Refresh token logic found" \
  || log_fail "No refresh token — JWT + refresh token rotation required"

MATCHES=$(grep -rn "deviceId" "$SRC/Features/Auth" --include="*.cs" 2>/dev/null \
  | grep -i "skip\|bypass\|disable\|ignore")
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "deviceId bypassing token rotation: $line"; done <<< "$MATCHES"
else log_pass "No deviceId bypass on token rotation"; fi

MATCHES=$(grep -rn 'AllowAnyOrigin\|origins\s*=\s*"\*"' "$SRC" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "Wildcard CORS: $line"; done <<< "$MATCHES"
else log_pass "No wildcard CORS"; fi

grep -rqn "RequestSizeLimit\|MaxRequestBodySize" "$SRC" --include="*.cs" 2>/dev/null \
  && log_pass "Request size limit configured" \
  || log_fail "No request size limit found"

MATCHES=$(grep -rn "HasQueryFilter" "$SRC" --include="*.cs" 2>/dev/null | grep -v "IsDeleted")
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "HasQueryFilter missing IsDeleted: $line"; done <<< "$MATCHES"
else log_pass "All HasQueryFilter include IsDeleted"; fi

# ─────────────────────────────────────────────
section "15. Audit Trail"
# ─────────────────────────────────────────────

grep -rn "AuditLog\|IAuditService\|CreatedBy\|UpdatedBy\|AuditTrail" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Audit trail found" \
  || log_fail "No audit trail — stock changes, order approvals, price overrides must be logged"

# ─────────────────────────────────────────────
section "16. Soft Delete Consistency"
# ─────────────────────────────────────────────

MATCHES=$(grep -rn "\.Remove(\|\.DeleteRange(" "$FEATURES_DIR" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "Hard delete found: $line"; done <<< "$MATCHES"
else log_pass "No hard deletes found"; fi

# ─────────────────────────────────────────────
section "17. Timezone Handling"
# ─────────────────────────────────────────────

MATCHES=$(grep -rn "DateTime\.Now\b" "$SRC" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "DateTime.Now used (use UtcNow): $line"; done <<< "$MATCHES"
else log_pass "No DateTime.Now found"; fi

MATCHES=$(grep -rn "timestamp without time zone" "$SRC/../Migrations" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "timestamp without time zone in migration: $line"; done <<< "$MATCHES"
else log_pass "No timestamp without time zone in migrations"; fi

# ─────────────────────────────────────────────
section "18. Architecture"
# ─────────────────────────────────────────────

MATCHES=$(grep -rn "MapPatch\|\[HttpPatch\]" "$FEATURES_DIR" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "PATCH used instead of PUT: $line"; done <<< "$MATCHES"
else log_pass "No PATCH endpoints found"; fi

MATCHES=$(grep -rn "IHttpContextAccessor\|HttpContext" "$FEATURES_DIR" --include="*Service*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "HttpContext in service layer: $line"; done <<< "$MATCHES"
else log_pass "No HttpContext in services"; fi

MATCHES=$(grep -rn "ApiResponse" "$FEATURES_DIR" --include="*Service*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "ApiResponse in service layer: $line"; done <<< "$MATCHES"
else log_pass "ApiResponse not used in services"; fi

MATCHES=$(grep -rn "FromSqlRaw\|ExecuteSqlRaw" "$SRC" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "Raw SQL without parameterization (use FromSqlInterpolated): $line"; done <<< "$MATCHES"
else log_pass "No raw SQL concatenation"; fi

# ─────────────────────────────────────────────
section "19. Observability"
# ─────────────────────────────────────────────

grep -rn "ILogger\|Serilog" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Structured logging found" \
  || log_fail "No ILogger/Serilog — never use Console.WriteLine"

MATCHES=$(grep -rn "Console\.WriteLine\|Console\.Write\b" "$SRC" --include="*.cs" 2>/dev/null \
  | grep -v "test\|Test")
if [ -n "$MATCHES" ]; then
  while IFS= read -r line; do log_fail "Console.WriteLine in production code: $line"; done <<< "$MATCHES"
else log_pass "No Console.WriteLine in production code"; fi

grep -rn "AddHealthChecks\|MapHealthChecks" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Health checks registered" \
  || log_fail "No health checks — /health endpoint required for load balancer"

grep -rn "log_min_duration\|DbCommandInterceptor\|slow.*query\|EF.*interceptor" --include="*.cs" "$SRC" > /dev/null 2>&1 \
  && log_pass "Slow query tracking found" \
  || log_fail "No slow query tracking — add EF Core interceptor or pg log_min_duration_statement"

# ─────────────────────────────────────────────
# SUMMARY
# ─────────────────────────────────────────────
echo "" >> "$REPORT"
echo "============================================" >> "$REPORT"
echo "TOTAL PASSED : $PASS" >> "$REPORT"
echo "TOTAL FAILED : $FAIL" >> "$REPORT"
echo "============================================" >> "$REPORT"

echo ""
echo "Audit complete — Scope: $SCOPE"
echo "PASSED: $PASS | FAILED: $FAIL"
echo "Report saved to: $REPORT"