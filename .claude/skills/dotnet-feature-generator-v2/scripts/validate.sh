#!/usr/bin/env bash
# Validate the generated .NET feature by running dotnet build and checking for anti-patterns.
# Run from the repo root: bash .claude/skills/dotnet-feature-generator-v2/scripts/validate.sh

set -euo pipefail

PROJECT_DIR="sfa_api/sfa_api"

if [ ! -d "$PROJECT_DIR" ]; then
  echo "ERROR: Could not find project at $PROJECT_DIR. Run from the repo root." >&2
  exit 1
fi

echo "==> Running dotnet build in $PROJECT_DIR..."
cd "$PROJECT_DIR"

BUILD_OUTPUT=$(dotnet build --no-restore 2>&1) || true

echo "$BUILD_OUTPUT"

# Check for build errors
if echo "$BUILD_OUTPUT" | grep -q "Build FAILED"; then
  echo ""
  echo "BUILD FAILED — fix errors above before proceeding."
  exit 1
fi

echo ""
echo "BUILD SUCCEEDED"

# Anti-pattern scan
echo ""
echo "==> Scanning for async anti-patterns..."
ANTI_PATTERNS=0

# Check for .Result
if grep -rn "\.Result[^s]" --include="*.cs" Features/ 2>/dev/null | grep -v "//"; then
  echo "WARNING: Found .Result usage — potential deadlock"
  ANTI_PATTERNS=$((ANTI_PATTERNS + 1))
fi

# Check for .Wait()
if grep -rn "\.Wait()" --include="*.cs" Features/ 2>/dev/null | grep -v "//"; then
  echo "WARNING: Found .Wait() usage — potential deadlock"
  ANTI_PATTERNS=$((ANTI_PATTERNS + 1))
fi

# Check for Task.Run
if grep -rn "Task\.Run(" --include="*.cs" Features/ 2>/dev/null | grep -v "//"; then
  echo "WARNING: Found Task.Run() — unnecessary thread pool usage"
  ANTI_PATTERNS=$((ANTI_PATTERNS + 1))
fi

# Check for IMemoryCache
if grep -rn "IMemoryCache" --include="*.cs" Features/ 2>/dev/null | grep -v "//"; then
  echo "WARNING: Found IMemoryCache — use IDistributedCache instead"
  ANTI_PATTERNS=$((ANTI_PATTERNS + 1))
fi

# Check for context.Remove
if grep -rn "\.Remove(" --include="*.cs" Features/ 2>/dev/null | grep -v "//" | grep -i "context\|_context"; then
  echo "WARNING: Found context.Remove() — use soft delete via IsActive = false"
  ANTI_PATTERNS=$((ANTI_PATTERNS + 1))
fi

# Check for AddDbContext (without Pool)
if grep -rn "AddDbContext<" --include="*.cs" . 2>/dev/null | grep -v "Pool" | grep -v "//"; then
  echo "WARNING: Found AddDbContext without Pool — use AddDbContextPool"
  ANTI_PATTERNS=$((ANTI_PATTERNS + 1))
fi

if [ $ANTI_PATTERNS -eq 0 ]; then
  echo "No anti-patterns found."
else
  echo ""
  echo "Found $ANTI_PATTERNS anti-pattern(s). Review and fix before marking feature complete."
  exit 1
fi

echo ""
echo "VALIDATION PASSED — build clean, no anti-patterns."
exit 0
