#!/usr/bin/env bash
# Smoke test for a running ContextSmith MCP server in HTTP mode.
# Usage: scripts/smoke-test.sh <base-url>     e.g. scripts/smoke-test.sh http://localhost:8080
# Exits 0 when GET <base-url>/health answers HTTP 200, and 1 otherwise.
set -euo pipefail

base_url="${1:?Usage: smoke-test.sh <base-url>}"
base_url="${base_url%/}"

status="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' --max-time 10 "${base_url}/health" || true)"

if [[ "${status}" == "200" ]]; then
  echo "OK: ${base_url}/health answered 200"
else
  echo "FAIL: ${base_url}/health answered '${status}' (expected 200)" >&2
  exit 1
fi
