#!/usr/bin/env bash
set -euo pipefail

if ! ls ./*.sln >/dev/null 2>&1; then
  echo "dotnet-format: no .sln file yet, skipping (see docs/PLAN.md Step 1)."
  exit 0
fi

dotnet format --verify-no-changes
