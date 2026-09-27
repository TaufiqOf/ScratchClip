#!/bin/bash
# ScratchClip Flatpak Build and Test Script
# Builds, installs, and optionally runs ScratchClip locally.

set -e

echo "🔧 ScratchClip Flatpak Build and Test"
echo "======================================"
echo ""

# ------------------------------------------------------------
# Check dependencies
# ------------------------------------------------------------

if ! command -v flatpak >/dev/null 2>&1; then
    echo "❌ flatpak is not installed"
    echo "Install it with:"
    echo "  sudo apt install flatpak"
    exit 1
fi

if ! command -v flatpak-builder >/dev/null 2>&1; then
    echo "❌ flatpak-builder is not installed"
    echo "Install it with:"
    echo "  sudo apt install flatpak-builder"
    exit 1
fi

# ------------------------------------------------------------
# Paths
# ------------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/flatpak-build-dir"
MANIFEST="$SCRIPT_DIR/io.github.TaufiqOf.ScratchClip.json"

if [ ! -f "$MANIFEST" ]; then
    echo "❌ Manifest file not found:"
    echo "   $MANIFEST"
    exit 1
fi

echo "📁 Working directory:"
echo "   $SCRIPT_DIR"
echo ""
echo "📋 Manifest:"
echo "   $MANIFEST"
echo ""

# ------------------------------------------------------------
# Flatpak / Flathub
# ------------------------------------------------------------

echo "🔎 Checking Flathub..."

if ! flatpak remote-list --system | awk '{print $1}' | grep -qx "flathub"; then
    echo "📦 Adding Flathub repository..."

    sudo flatpak remote-add \
        --if-not-exists \
        flathub \
        https://flathub.org/repo/flathub.flatpakrepo
fi

echo "✅ Flathub available"
echo ""

# ------------------------------------------------------------
# Arguments
# ------------------------------------------------------------

CLEAN_BUILD=false
RUN_APP=true
VERBOSE=false

for arg in "$@"; do
    case "$arg" in
        clean|--clean)
            CLEAN_BUILD=true
            ;;
        --no-run)
            RUN_APP=false
            ;;
        --verbose)
            VERBOSE=true
            ;;
        --help|-h)
            echo "Usage:"
            echo "  ./build-flatpak.sh"
            echo "  ./build-flatpak.sh clean"
            echo "  ./build-flatpak.sh --no-run"
            echo "  ./build-flatpak.sh --verbose"
            echo "  ./build-flatpak.sh clean --verbose"
            exit 0
            ;;
        *)
            echo "⚠️ Unknown argument: $arg"
            echo "Use --help for usage information."
            exit 1
            ;;
    esac
done

# ------------------------------------------------------------
# Clean
# ------------------------------------------------------------

if [ "$CLEAN_BUILD" = true ]; then
    echo "🧹 Cleaning previous build..."

    rm -rf "$BUILD_DIR"

    echo "✅ Build directory cleaned"
    echo ""
fi

# ------------------------------------------------------------
# Build
# ------------------------------------------------------------

echo "🔨 Building ScratchClip Flatpak..."
echo "This may take several minutes..."
echo ""

BUILD_ARGS=(
    --user
    --install
    --force-clean
    "$BUILD_DIR"
    "$MANIFEST"
)

if [ "$VERBOSE" = true ]; then
    BUILD_ARGS=(
        --verbose
        "${BUILD_ARGS[@]}"
    )
fi

if flatpak-builder "${BUILD_ARGS[@]}"; then
    echo ""
    echo "✅ Flatpak build successful!"
    echo ""
else
    echo ""
    echo "❌ Flatpak build failed!"
    echo ""
    echo "Try:"
    echo ""
    echo "  ./build-flatpak.sh clean --verbose"
    echo ""
    echo "Or manually:"
    echo ""
    echo "  flatpak-builder --verbose --user --install --force-clean \\"
    echo "    \"$BUILD_DIR\" \\"
    echo "    \"$MANIFEST\""
    echo ""

    exit 1
fi

# ------------------------------------------------------------
# Verify installation
# ------------------------------------------------------------

echo "🔍 Verifying installed application..."

if flatpak info com.github.taufiq.scratchclip >/dev/null 2>&1; then
    echo "✅ ScratchClip is installed"
else
    echo "❌ ScratchClip was not found after installation"
    exit 1
fi

echo ""

# ------------------------------------------------------------
# Run application
# ------------------------------------------------------------

if [ "$RUN_APP" = true ]; then
    echo "🚀 ScratchClip is ready"
    echo ""

    read -r -p "Would you like to run ScratchClip now? (y/n) " REPLY
    echo ""

    if [[ "$REPLY" =~ ^[Yy]$ ]]; then
        echo "🚀 Launching ScratchClip..."
        echo ""

        if flatpak run com.github.taufiq.scratchclip; then
            echo ""
            echo "✅ ScratchClip exited normally"
        else
            echo ""
            echo "⚠️ ScratchClip exited with an error"
            echo ""
            echo "Run the debug version with:"
            echo ""
            echo "  flatpak run --devel com.github.taufiq.scratchclip"
            echo ""
            echo "Or open a shell inside the Flatpak:"
            echo ""
            echo "  flatpak run --command=sh --devel com.github.taufiq.scratchclip"
        fi
    fi
fi

# ------------------------------------------------------------
# Finish
# ------------------------------------------------------------

echo ""
echo "======================================"
echo "✨ Done!"
echo ""
echo "Run manually:"
echo "  flatpak run com.github.taufiq.scratchclip"
echo ""
echo "Debug:"
echo "  flatpak run --devel com.github.taufiq.scratchclip"
echo ""
echo "Debug shell:"
echo "  flatpak run --command=sh --devel com.github.taufiq.scratchclip"
echo ""
echo "Uninstall:"
echo "  flatpak uninstall com.github.taufiq.scratchclip"
echo ""