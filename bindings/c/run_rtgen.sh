#!/usr/bin/bash
# Generates the C bindings.  rtgen (--language=c --config) discovers every core
# header that declares an interface or class factory, parses each with the C++
# parser (retrying unparseable headers from a sanitized copy), and emits a
# per-interface include/<lib>/<name>.h + src/<lib>/<name>.cpp.  Discovery is the
# single source of truth - there is no hand-maintained header list any more; the
# exceptions (name-collision renames, forbidden types/factories, platform-guarded
# and factory-only headers) are encoded in the CConfigGenerator / CGenerator.
set -eu

BINDINGS_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(cd "$BINDINGS_DIR/../.." && pwd)"

RTGEN="$REPO_DIR/shared/tools/RTGen/bin/rtgen.exe"
if command -v mono &>/dev/null; then
    RTGEN="mono $RTGEN"
fi

# Run from the repository root so header discovery uses repository-relative paths.
cd "$REPO_DIR"
exec $RTGEN --language=c --config --namespace=daq --source=core \
     --outputDir=bindings/c
