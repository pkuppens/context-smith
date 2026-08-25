#!/usr/bin/env bash
set -euo pipefail

if ! ls ./*.sln >/dev/null 2>&1; then
  echo "dotnet-test: no .sln file yet, skipping (see docs/PLAN.md Step 1)."
  exit 0
fi

dotnet test --nologo
