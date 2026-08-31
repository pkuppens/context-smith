#!/usr/bin/env bash
set -euo pipefail

# CS1591 (missing XML doc comment on a public API) is promoted to an error in
# .editorconfig, so this build fails when a public type or member in src/ has no
# XML doc comment. See issue #26.
dotnet build --nologo
