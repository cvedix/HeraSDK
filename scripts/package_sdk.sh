#!/bin/bash
# package_sdk.sh — Package Core Runtime build into SDK distribution
#
# Usage:
#   cd /path/to/core
#   ./path/to/cvedix-sdk/scripts/package_sdk.sh [--sdk-dir /path/to/cvedix-sdk]
#
# Prerequisites: Core Runtime must be built first (make build)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SDK_DIR="${1:-$(dirname "$SCRIPT_DIR")}"
CORE_DIR="${CORE_DIR:-$(pwd)}"

echo "╔════════════════════════════════════════════╗"
echo "║  CVEDIX SDK Packager                       ║"
echo "╚════════════════════════════════════════════╝"
echo ""
echo "  Core dir: $CORE_DIR"
echo "  SDK dir:  $SDK_DIR"
echo ""

# ── Validate core build ──
if [ ! -d "$CORE_DIR/build/libs" ]; then
    echo "ERROR: Core build not found at $CORE_DIR/build/libs"
    echo "       Run 'make build' in core directory first."
    exit 1
fi

if [ ! -d "$CORE_DIR/deb_package/opt/cvedix/include" ]; then
    echo "ERROR: Public headers not found at $CORE_DIR/deb_package/opt/cvedix/include"
    exit 1
fi

# ── Detect architecture ──
ARCH=$(uname -m)
echo "  Architecture: $ARCH"
echo ""

# ── Copy public headers ──
echo "[1/4] Copying public headers..."
rm -rf "$SDK_DIR/include"
cp -r "$CORE_DIR/deb_package/opt/cvedix/include" "$SDK_DIR/include"
HEADER_COUNT=$(find "$SDK_DIR/include" -name "*.h" | wc -l)
echo "       → $HEADER_COUNT header files copied"

# ── Copy pre-built libraries ──
echo "[2/4] Copying pre-built libraries..."
LIB_DIR="$SDK_DIR/lib/$ARCH"
mkdir -p "$LIB_DIR"
cp -f "$CORE_DIR/build/libs/"*.so "$LIB_DIR/" 2>/dev/null || true
LIB_COUNT=$(find "$LIB_DIR" -name "*.so" | wc -l)
echo "       → $LIB_COUNT shared libraries copied to lib/$ARCH/"

# ── Copy C API header (foundation for Java/C#/C bindings) ──
if [ -f "$CORE_DIR/capi/include/cvedix_c_api.h" ]; then
    mkdir -p "$SDK_DIR/include/cvedix/capi"
    cp -f "$CORE_DIR/capi/include/cvedix_c_api.h" "$SDK_DIR/include/cvedix/capi/"
    echo "       → C API header copied to include/cvedix/capi/"
fi

# ── Strip debug symbols (reduce size) ──
echo "[3/4] Stripping debug symbols..."
for so in "$LIB_DIR"/*.so; do
    if [ -f "$so" ]; then
        strip --strip-debug "$so" 2>/dev/null || true
    fi
done
TOTAL_SIZE=$(du -sh "$LIB_DIR" | cut -f1)
echo "       → Total lib size: $TOTAL_SIZE"

# ── Generate version info ──
echo "[4/4] Generating version info..."
VERSION_FILE="$SDK_DIR/VERSION"
if [ -d "$CORE_DIR/.git" ]; then
    cd "$CORE_DIR"
    COMMIT=$(git rev-parse --short HEAD 2>/dev/null || echo "unknown")
    DATE=$(date +%Y%m%d)
    VERSION="sdk-$DATE-$COMMIT"
    cd - > /dev/null
else
    VERSION="sdk-$(date +%Y%m%d)-local"
fi
echo "$VERSION" > "$VERSION_FILE"
echo "       → Version: $VERSION"

echo ""
echo "✅ SDK packaged successfully!"
echo "   Headers: $SDK_DIR/include/"
echo "   Libs:    $SDK_DIR/lib/$ARCH/"
echo "   Version: $VERSION"
