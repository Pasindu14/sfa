#!/usr/bin/env bash
# Validate the generated Next.js feature with TypeScript type-check.
# Run from the repo root: bash .claude/skills/nextjs-sfa-full-feature/scripts/validate.sh

set -euo pipefail

WEB_DIR="sfa_web"

if [ ! -d "$WEB_DIR" ]; then
  echo "ERROR: Could not find $WEB_DIR. Run from the repo root." >&2
  exit 1
fi

echo "==> Running TypeScript type check in $WEB_DIR..."
cd "$WEB_DIR"

npx tsc --noEmit 2>&1

exit_code=$?

if [ $exit_code -eq 0 ]; then
  echo ""
  echo "✓ Type check passed — no errors."
else
  echo ""
  echo "✗ Type check failed (exit code $exit_code). Fix TypeScript errors before proceeding."
fi

exit $exit_code
