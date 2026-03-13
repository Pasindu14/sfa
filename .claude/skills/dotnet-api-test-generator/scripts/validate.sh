#!/usr/bin/env bash
# Run unit and integration tests for the SFA API.
# Usage: bash .claude/skills/dotnet-api-test-generator/scripts/validate.sh [unit|integration|all]
# Default: all

set -euo pipefail

ROOT="sfa_api"
UNIT_PROJ="$ROOT/sfa_api.UnitTests"
INTEGRATION_PROJ="$ROOT/sfa_api.IntegrationTests"

MODE="${1:-all}"

run_tests() {
  local proj="$1"
  local label="$2"
  echo ""
  echo "==> Running $label tests in $proj..."
  dotnet test "$proj" --logger "console;verbosity=normal" 2>&1
}

case "$MODE" in
  unit)
    run_tests "$UNIT_PROJ" "unit"
    ;;
  integration)
    run_tests "$INTEGRATION_PROJ" "integration"
    ;;
  all|*)
    run_tests "$UNIT_PROJ" "unit"
    run_tests "$INTEGRATION_PROJ" "integration"
    ;;
esac

echo ""
echo "✓ Test run complete."
