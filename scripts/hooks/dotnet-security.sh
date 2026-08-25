#!/usr/bin/env bash
set -euo pipefail

if ! ls ./*.sln >/dev/null 2>&1; then
  echo "dotnet-vulnerable-packages: no .sln file yet, skipping (see docs/PLAN.md Step 1)."
  exit 0
fi

output=$(dotnet list package --vulnerable --include-transitive 2>&1)
echo "$output"

if echo "$output" | grep -qi "has the following vulnerable packages"; then
  echo "dotnet-vulnerable-packages: vulnerable NuGet package(s) found. Update or replace them." >&2
  exit 1
fi
