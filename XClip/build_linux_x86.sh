#!/usr/bin/env bash

set -euo pipefail

# ============================================================
# ClipboardManagerX - Linux AppImage build script
# ============================================================

APP_NAME="XClip"
PROJECT_FILE="XClip.csproj"

RUNTIME="linux-x64"
CONFIGURATION="Release"
FRAMEWORK="net10.0"

OUTPUT_DIR="dist/linux"
PUBLISH_DIR="$OUTPUT_DIR/publish"
APPDIR="$OUTPUT_DIR/AppDir"

APPIMAGE="$OUTPUT_DIR/${APP_NAME}-x86_64.AppImage"

ICON_SOURCE="Assets/icon-light.png"
ICON_NAME="XClip"

DESKTOP_FILE="$APPDIR/$ICON_NAME.desktop"

APPIMAGETOOL="$OUTPUT_DIR/appimagetool"
APPIMAGETOOL_URL="https://github.com/AppImage/appimagetool/releases/latest/download/appimagetool-x86_64.AppImage"


echo
echo "============================================================"
echo " Building $APP_NAME AppImage"
echo "============================================================"
echo


# ============================================================
# Check prerequisites
# ============================================================

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet was not found."
    exit 1
fi

if ! command -v wget >/dev/null 2>&1; then
    echo "ERROR: wget was not found."
    echo
    echo "Install with:"
    echo
    echo "  sudo apt install wget"
    exit 1
fi

if [ ! -f "$PROJECT_FILE" ]; then
    echo "ERROR: $PROJECT_FILE was not found."
    exit 1
fi

if [ ! -f "$ICON_SOURCE" ]; then
    echo "ERROR: Icon not found:"
    echo "  $ICON_SOURCE"
    exit 1
fi


# ============================================================
# Clean
# ============================================================

echo "==> Cleaning previous build..."

rm -rf "$OUTPUT_DIR"

mkdir -p "$PUBLISH_DIR"
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"


# ============================================================
# Restore
# ============================================================

echo
echo "==> Restoring dependencies..."

dotnet restore "$PROJECT_FILE" \
    -r "$RUNTIME"


# ============================================================
# Publish
# ============================================================

echo
echo "==> Publishing "$APP_NAME"..."

# Note: IncludeNativeLibrariesForSelfExtract ensures native assets 
# are emitted to the publish directory during the publish step.
dotnet publish "$PROJECT_FILE" \
    -c "$CONFIGURATION" \
    -f "$FRAMEWORK" \
    -r "$RUNTIME" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:EnableCompressionInSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:IncludeAllContentForSelfExtract=true \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -o "$PUBLISH_DIR"


# ============================================================
# Check executable
# ============================================================

GENERATED_EXECUTABLE="$PUBLISH_DIR/$APP_NAME"

if [ ! -f "$GENERATED_EXECUTABLE" ]; then
    echo
    echo "ERROR: "$APP_NAME" executable was not generated."
    exit 1
fi


# ============================================================
# Copy executable & Native Libraries
# ============================================================

echo
echo "==> Installing executable and native dependencies..."

cp -a "$PUBLISH_DIR/." "$APPDIR/usr/bin/"

# Copy any extracted or unmanaged native libraries from NuGet cache if missing
NUGET_NATIVE_DIR="$HOME/.nuget/packages/treesitter.dotnet"
if [ -d "$NUGET_NATIVE_DIR" ]; then
    find "$NUGET_NATIVE_DIR" -type f -name "*.so" -exec cp {} "$APPDIR/usr/bin/" \; 2>/dev/null || true
fi

chmod +x "$APPDIR/usr/bin/$APP_NAME"


# ============================================================
# Create AppRun (with LD_LIBRARY_PATH configured)
# ============================================================

echo
echo "==> Creating AppRun..."

cat > "$APPDIR/AppRun" <<EOF
#!/usr/bin/env bash

HERE="\$(dirname "\$(readlink -f "\$0")")"

# Export shared library paths so DllImport finds TreeSitter .so binaries
export LD_LIBRARY_PATH="\$HERE/usr/bin:\$LD_LIBRARY_PATH"

exec "\$HERE/usr/bin/$APP_NAME" "\$@"
EOF

chmod +x "$APPDIR/AppRun"


# ============================================================
# Install icon
# ============================================================

echo
echo "==> Installing icon..."

cp "$ICON_SOURCE" \
    "$APPDIR/$APP_NAME.png"

cp "$ICON_SOURCE" \
    "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_NAME.png"


# ============================================================
# Create desktop file
# ============================================================

echo
echo "==> Creating desktop entry..."

DESKTOP_FILE="$APPDIR/$APP_NAME.desktop"

cat > "$DESKTOP_FILE" <<EOF
[Desktop Entry]
Name=$APP_NAME
Comment=Clipboard Manager
Exec=$APP_NAME
Icon=$APP_NAME
Terminal=false
Type=Application
Categories=Utility;
StartupNotify=true
EOF


# ============================================================
# Show AppDir
# ============================================================

echo
echo "==> AppDir structure:"
echo

find "$APPDIR" -type f -printf '  %P\n'

echo


# ============================================================
# Download appimagetool
# ============================================================

if [ ! -f "$APPIMAGETOOL" ]; then

    echo "==> Downloading appimagetool..."

    wget \
        -O "$APPIMAGETOOL" \
        "$APPIMAGETOOL_URL"

    chmod +x "$APPIMAGETOOL"

else

    echo "==> Using existing appimagetool..."

fi


# ============================================================
# Create AppImage
# ============================================================

echo
echo "==> Creating AppImage..."

rm -f "$APPIMAGE"

ARCH=x86_64 "$APPIMAGETOOL" \
    "$APPDIR" \
    "$APPIMAGE"


# ============================================================
# Verify
# ============================================================

if [ ! -f "$APPIMAGE" ]; then
    echo
    echo "ERROR: AppImage was not created."
    exit 1
fi

chmod +x "$APPIMAGE"


# ============================================================
# Cleanup
# ============================================================

echo
echo "==> Cleaning temporary files..."

rm -rf "$PUBLISH_DIR"
rm -rf "$APPDIR"
rm -f "$APPIMAGETOOL"


# ============================================================
# Result
# ============================================================

echo
echo "============================================================"
echo " AppImage created successfully!"
echo "============================================================"
echo
echo "Output:"
echo
echo "  $APPIMAGE"
echo

ls -lh "$APPIMAGE"

echo
echo "Run with:"
echo
echo "  ./$APPIMAGE"
echo