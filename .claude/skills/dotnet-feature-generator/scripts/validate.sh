#!/usr/bin/env bash
# Validate the generated .NET feature by running dotnet build.
# Run from the repo root: bash .claude/skills/dotnet-feature-generator/scripts/validate.sh

set -euo pipefail

PROJECT_DIR="sfa_api/sfa_api"

if [ ! -d "$PROJECT_DIR" ]; then
  echo "ERROR: Could not find project at $PROJECT_DIR. Run from the repo root." >&2
  exit 1
fi

echo "==> Running dotnet build in $PROJECT_DIR..."
cd "$PROJECT_DIR"

dotnet build --no-restore 2>&1

exit_code=$?

if [ $exit_code -eq 0 ]; then
  echo ""
  echo "✓ Build succeeded — no errors."
else
  echo ""
  echo "✗ Build failed (exit code $exit_code). Review errors above and fix before proceeding."
fi

exit $exit_code
