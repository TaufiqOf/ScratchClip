#!/bin/bash
# Quick Flatpak Build and Test Script for ScratchClip
# This script builds and tests the ScratchClip Flatpak locally

set -e

echo "🔧 ScratchClip Flatpak Build and Test"
echo "======================================"
echo ""

# Check if flatpak is installed
if ! command -v flatpak &> /dev/null; then
    echo "❌ flatpak is not installed"
    echo "Install it with: sudo apt install flatpak flatpak-builder"
    exit 1
fi

# Check if flatpak-builder is installed
if ! command -v flatpak-builder &> /dev/null; then
    echo "❌ flatpak-builder is not installed"
    echo "Install it with: sudo apt install flatpak-builder"
    exit 1
fi

# Add Flathub repo if not present
if ! flatpak remote-list --system | grep -q "flathub"; then
    echo "📦 Adding Flathub repository..."
    flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/flatpak-build-dir"
MANIFEST="$SCRIPT_DIR/com.github.taufiq.scratchclip.json"

if [ ! -f "$MANIFEST" ]; then
    echo "❌ Manifest file not found: $MANIFEST"
    exit 1
fi

echo "📁 Working directory: $SCRIPT_DIR"
echo "📋 Manifest file: $MANIFEST"
echo ""

# Clean previous build if requested
if [ "$1" = "clean" ] || [ "$1" = "--clean" ]; then
    echo "🧹 Cleaning previous build..."
    rm -rf "$BUILD_DIR"
    echo "✅ Clean complete"
    echo ""
fi

# Build the Flatpak
echo "🔨 Building ScratchClip Flatpak..."
echo "This may take several minutes..."
echo ""

if flatpak-builder --user --install --force-clean "$BUILD_DIR" "$MANIFEST"; then
    echo ""
    echo "✅ Build successful!"
    echo ""
    
    # Ask if user wants to run the app
    read -p "Would you like to run ScratchClip now? (y/n) " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "🚀 Launching ScratchClip..."
        echo ""
        
        if flatpak run com.github.taufiq.scratchclip; then
            echo "✅ Application closed successfully"
        else
            echo "⚠️ Application exited with an error"
            echo ""
            echo "For debugging, run with:"
            echo "  flatpak run --devel com.github.taufiq.scratchclip"
        fi
    fi
else
    echo ""
    echo "❌ Build failed!"
    echo ""
    echo "Troubleshooting steps:"
    echo "1. Check the error messages above"
    echo "2. Ensure you have the latest Flatpak:"
    echo "   flatpak update"
    echo "3. Try again with verbose output:"
    echo "   flatpak-builder --verbose --user --install --force-clean \"$BUILD_DIR\" \"$MANIFEST\""
    echo ""
    exit 1
fi

echo ""
echo "======================================"
echo "✨ Done!"
echo ""
echo "To run ScratchClip manually:"
echo "  flatpak run com.github.taufiq.scratchclip"
echo ""
echo "To uninstall:"
echo "  flatpak uninstall com.github.taufiq.scratchclip"
echo ""
echo "To view logs:"
echo "  flatpak run --devel com.github.taufiq.scratchclip"
echo ""

