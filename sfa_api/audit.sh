#!/bin/bash

# ─────────────────────────────────────────────
# SFA API — Production Readiness Audit Script
# Run from: E:\Github\sfa\sfa_api
# Output:   audit-report.txt
# ─────────────────────────────────────────────

SRC="./sfa_api"
REPORT="./audit-report.txt"
PASS=0
FAIL=0

# Optional feature filter — e.g. bash audit.sh Areas
FEATURE="${1:-}"
if [ -n "$FEATURE" ]; then
  FEATURES_DIR="$SRC/Features/$FEATURE"
  if [ ! -d "$FEATURES_DIR" ]; then
    echo "ERROR: Feature directory not found: $FEATURES_DIR"
    exit 1
  fi
  echo "Auditing feature: $FEATURE"
else
  FEATURES_DIR="$FEATURES_DIR"
fi

> "$REPORT"

log_pass() { echo "  PASS — $1" >> "$REPORT"; ((PASS++)); }
log_fail() { echo "  FAIL — $1" >> "$REPORT"; ((FAIL++)); }

section() { echo "" >> "$REPORT"; echo "## $1" >> "$REPORT"; }

SCOPE="${FEATURE:-ALL FEATURES}"
echo "SFA API Audit Report — $(date) [Scope: $SCOPE]" >> "$REPORT"
echo "============================================" >> "$REPORT"

# ─────────────────────────────────────────────
section "1. Error Handling"
# ─────────────────────────────────────────────

# try-catch in endpoint files
FILES=$(grep -rl "try" "$FEATURES_DIR" --include="*Endpoint*.cs" --include="*Controller*.cs" 2>/dev/null)
if [ -n "$FILES" ]; then
  for f in $FILES; do log_fail "try-catch in endpoint: $f"; done
else log_pass "No try-catch in endpoints"; fi

# Untyped exceptions (generic Exception throw)
MATCHES=$(grep -rn "throw new Exception(" "$FEATURES_DIR" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while read line; do log_fail "Untyped exception: $line"; done
else log_pass "No untyped exceptions"; fi

# FluentValidation on all request DTOs
VALIDATORS=$(find "$FEATURES_DIR" -name "*Validator*.cs" | wc -l)
REQUESTS=$(find "$FEATURES_DIR" -name "*Request*.cs" | wc -l)
if [ "$VALIDATORS" -lt "$REQUESTS" ]; then
  log_fail "Possible missing validators: $VALIDATORS validators vs $REQUESTS request files"
else log_pass "Validator count matches request count ($VALIDATORS/$REQUESTS)"; fi

# ─────────────────────────────────────────────
section "2. Performance"
# ─────────────────────────────────────────────

# Missing AsNoTracking on read queries
MATCHES=$(grep -rn "\.Where\|\.FirstOrDefault\|\.ToList\|\.ToListAsync" "$FEATURES_DIR" --include="*Repository*.cs" -l 2>/dev/null)
for f in $MATCHES; do
  if ! grep -q "AsNoTracking" "$f"; then
    log_fail "Missing AsNoTracking in: $f"
  else log_pass "AsNoTracking present: $f"; fi
done

# Unbounded queries (no Take/pagination)
MATCHES=$(grep -rn "\.ToListAsync" "$FEATURES_DIR" --include="*Repository*.cs" 2>/dev/null | grep -v "Take\|Skip\|Paginate\|cursor")
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "Possible unbounded query: $line"; done
else log_pass "No unbounded ToListAsync found"; fi

# String concatenation in logs
MATCHES=$(grep -rn 'ILogger.*\$"' "$SRC" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "String interpolation in log: $line"; done
else log_pass "No string interpolation in logs"; fi

# ─────────────────────────────────────────────
section "3. Security"
# ─────────────────────────────────────────────

# Missing [Authorize] on endpoints/controllers
FILES=$(find "$FEATURES_DIR" -name "*Endpoint*.cs" -o -name "*Controller*.cs" 2>/dev/null)
for f in $FILES; do
  if ! grep -q "\[Authorize" "$f"; then
    log_fail "Missing [Authorize]: $f"
  else log_pass "[Authorize] present: $f"; fi
done

# Hardcoded secrets
MATCHES=$(grep -rn "password\s*=\s*['\"].\|secret\s*=\s*['\"].\|apikey\s*=\s*['\"]." "$SRC" --include="*.cs" -i 2>/dev/null)
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "Hardcoded secret: $line"; done
else log_pass "No hardcoded secrets found"; fi

# HasQueryFilter missing IsDeleted
MATCHES=$(grep -rn "HasQueryFilter" "$SRC" --include="*.cs" 2>/dev/null | grep -v "IsDeleted")
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "HasQueryFilter missing IsDeleted: $line"; done
else log_pass "All HasQueryFilter include IsDeleted"; fi

# deviceId disabling token rotation
MATCHES=$(grep -rn "deviceId" "$SRC/Features/Auth" --include="*.cs" 2>/dev/null | grep -i "skip\|bypass\|disable\|ignore")
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "deviceId bypassing token rotation: $line"; done
else log_pass "No deviceId bypass on token rotation"; fi

# ─────────────────────────────────────────────
section "4. Architecture"
# ─────────────────────────────────────────────

# CancellationToken missing in async methods
MATCHES=$(grep -rn "async Task\|async IActionResult\|async IResult" "$FEATURES_DIR" --include="*.cs" 2>/dev/null | grep -v "CancellationToken")
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "Missing CancellationToken: $line"; done
else log_pass "All async methods have CancellationToken"; fi

# PATCH used instead of PUT
MATCHES=$(grep -rn "MapPatch\|\[HttpPatch\]" "$FEATURES_DIR" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "PATCH used instead of PUT: $line"; done
else log_pass "No PATCH endpoints found"; fi

# HttpContext in service layer
MATCHES=$(grep -rn "IHttpContextAccessor\|HttpContext" "$FEATURES_DIR" --include="*Service*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "HttpContext in service: $line"; done
else log_pass "No HttpContext in services"; fi

# ApiResponse wrapping in service layer
MATCHES=$(grep -rn "ApiResponse" "$FEATURES_DIR" --include="*Service*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "ApiResponse in service layer: $line"; done
else log_pass "ApiResponse not used in services"; fi

# ─────────────────────────────────────────────
section "5. Data Access"
# ─────────────────────────────────────────────

# AddDbContext instead of AddDbContextPool
MATCHES=$(grep -rn "AddDbContext\b" "$SRC" --include="*.cs" 2>/dev/null | grep -v "AddDbContextPool")
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "AddDbContext used instead of AddDbContextPool: $line"; done
else log_pass "AddDbContextPool used"; fi

# Raw string SQL concatenation
MATCHES=$(grep -rn "FromSqlRaw\|ExecuteSqlRaw" "$SRC" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "Raw SQL (use FromSqlInterpolated): $line"; done
else log_pass "No raw SQL concatenation"; fi

# ─────────────────────────────────────────────
section "6. Timezone Handling"
# ─────────────────────────────────────────────

# DateTime.Now usage (should be UtcNow)
MATCHES=$(grep -rn "DateTime\.Now\b" "$SRC" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "DateTime.Now used: $line"; done
else log_pass "No DateTime.Now found"; fi

# Missing timestamptz in migrations
MATCHES=$(grep -rn "timestamp without time zone" "$SRC/../Migrations" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "timestamp without time zone in migration: $line"; done
else log_pass "No timestamp without time zone in migrations"; fi

# ─────────────────────────────────────────────
section "7. Request Hardening"
# ─────────────────────────────────────────────

# Wildcard CORS
MATCHES=$(grep -rn 'AllowAnyOrigin\|origins\s*=\s*"\*"' "$SRC" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "Wildcard CORS: $line"; done
else log_pass "No wildcard CORS"; fi

# Request size limit
if grep -rqn "RequestSizeLimit\|MaxRequestBodySize" "$SRC" ./Program.cs --include="*.cs" 2>/dev/null; then
  log_pass "Request size limit configured"
else log_fail "No request size limit found in Program.cs"; fi

# ─────────────────────────────────────────────
section "8. Soft Delete Consistency"
# ─────────────────────────────────────────────

# Hard delete anywhere
MATCHES=$(grep -rn "\.Remove(\|\.DeleteRange(" "$FEATURES_DIR" --include="*.cs" 2>/dev/null)
if [ -n "$MATCHES" ]; then
  echo "$MATCHES" | while IFS= read -r line; do log_fail "Hard delete found: $line"; done
else log_pass "No hard deletes found"; fi

# ─────────────────────────────────────────────
# SUMMARY
# ─────────────────────────────────────────────
echo "" >> "$REPORT"
echo "============================================" >> "$REPORT"
echo "TOTAL PASSED : $PASS" >> "$REPORT"
echo "TOTAL FAILED : $FAIL" >> "$REPORT"
echo "============================================" >> "$REPORT"

echo ""
echo "Audit complete. PASSED: $PASS | FAILED: $FAIL"
echo "Report saved to: $REPORT"