#!/usr/bin/env bash
# Regenerates everything under bindings/ that is derived from the core C++ headers:
#   bindings/c/{include,src}      the C bindings      (RTGen.C)
#   bindings/json/opendaq.json    the API description (RTGen.Json)
#
#   bindings/regenerate.sh [--no-build]
#
# The two generators are rebuilt from source first, so the committed RTGen.C.dll / RTGen.Json.dll can
# never be stale with respect to their sources. This needs the mono C# compiler (mcs); pass --no-build
# to run the committed binaries instead, for which the mono runtime is enough.
set -euo pipefail

repo="$(cd "$(dirname "$0")/.." && pwd)"
rtgen="$repo/shared/tools/RTGen"
projects="$rtgen/src/project"

build_generator() {
    local name="$1"
    local sources=()
    while IFS= read -r file; do
        sources+=("$file")
    done < <(find "$projects/RTGen.$name" "$projects/RTGen.Shared" -name '*.cs' \
                  -not -path '*/obj/*' -not -path '*/bin/*' 2>/dev/null | LC_ALL=C sort)

    mcs -nologo -nowarn:1591 -target:library -out:"$rtgen/bin/RTGen.$name.dll" \
        -r:"$rtgen/bin/RTGen.Library.dll" -r:"$rtgen/bin/RTGen.Cpp.dll" \
        -r:"$rtgen/bin/LightInject.dll" -r:System.Core.dll \
        "${sources[@]}"

    # rtgen.exe reads the templates next to itself; the ones in the project are the source of truth.
    if [ -d "$projects/RTGen.$name/Templates" ]; then
        cp "$projects/RTGen.$name/Templates/"* "$rtgen/bin/Templates/"
    fi
}

if [ "${1:-}" != "--no-build" ]; then
    if ! command -v mcs >/dev/null 2>&1; then
        echo "mcs (the mono C# compiler) is not installed; install it or pass --no-build." >&2
        exit 1
    fi
    build_generator C
    build_generator Json
fi

bash "$repo/bindings/c/run_rtgen.sh"
bash "$repo/bindings/json/run_rtgen.sh"
